using SmartOrder.Shared.Services;
using System.Diagnostics;
using System.Text;

namespace SmartOrder.Shared.Printing;

public sealed class WindowsPrinterDiscoveryService : IWindowsPrinterDiscoveryService
{
    public const string DefaultUsbPrinterName = "SmartOrder Ticket USB";

    public async Task<IReadOnlyList<string>> GetSerialPortsAsync(CancellationToken cancellationToken = default)
    {
        var result = await RunPowerShellAsync("[System.IO.Ports.SerialPort]::GetPortNames() | Sort-Object", cancellationToken);
        return result.Success
            ? result.Output
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(port => port.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : Array.Empty<string>();
    }

    public async Task<IReadOnlyList<PrinterInfo>> GetPrintersAsync(CancellationToken cancellationToken = default)
    {
        const string script = "Get-Printer | ForEach-Object { \"$($_.Name)|$($_.PortName)|$($_.DriverName)\" }";
        var result = await RunPowerShellAsync(script, cancellationToken);
        if (!result.Success)
        {
            return Array.Empty<PrinterInfo>();
        }

        return result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParsePrinterInfo)
            .Where(printer => printer != null)
            .Select(printer => printer!)
            .OrderBy(printer => printer.Name)
            .ToList();
    }

    public async Task<PrinterConfigurationResult> ConfigureDefaultUsbPrinterAsync(CancellationToken cancellationToken = default)
    {
        var escapedPrinterName = DefaultUsbPrinterName.Replace("'", "''", StringComparison.Ordinal);
        var script = $$"""
$ErrorActionPreference = 'Stop'
$printerName = '{{escapedPrinterName}}'
$port = Get-PrinterPort | Where-Object { $_.Name -like 'USB*' } | Sort-Object Name | Select-Object -First 1
if ($null -eq $port) { throw 'No se encontro un puerto USB de impresora. Conecta y enciende la impresora.' }
if (-not (Get-PrinterDriver -Name 'Generic / Text Only' -ErrorAction SilentlyContinue)) {
    rundll32 printui.dll,PrintUIEntry /ia /m 'Generic / Text Only' /h 'x64' /v 'Type 3 - User Mode' /f "$env:windir\inf\ntprint.inf"
}
for ($i = 0; $i -lt 10 -and -not (Get-PrinterDriver -Name 'Generic / Text Only' -ErrorAction SilentlyContinue); $i++) {
    Start-Sleep -Milliseconds 500
}
if (-not (Get-PrinterDriver -Name 'Generic / Text Only' -ErrorAction SilentlyContinue)) {
    throw 'No se pudo preparar el driver Generic / Text Only.'
}
if (-not (Get-Printer -Name $printerName -ErrorAction SilentlyContinue)) {
    Add-Printer -Name $printerName -DriverName 'Generic / Text Only' -PortName $port.Name
}
else {
    Set-Printer -Name $printerName -PortName $port.Name
}
"$printerName|$($port.Name)"
""";

        var result = await RunPowerShellAsync(script, cancellationToken);
        if (!result.Success)
        {
            FileErrorLogger.Log("WindowsPrinterDiscoveryService.ConfigureDefaultUsbPrinterAsync", new InvalidOperationException(result.Error));
            return new PrinterConfigurationResult(
                false,
                "No se pudo configurar la impresora USB. Ejecuta la app como administrador o instala la impresora manualmente como 'SmartOrder Ticket USB'.");
        }

        var parts = result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault()?
            .Split('|');

        return new PrinterConfigurationResult(
            true,
            $"Impresora USB configurada correctamente en {parts?.ElementAtOrDefault(1) ?? "USB"}.",
            parts?.ElementAtOrDefault(0) ?? DefaultUsbPrinterName,
            parts?.ElementAtOrDefault(1));
    }

    private static PrinterInfo? ParsePrinterInfo(string value)
    {
        var parts = value.Split('|');
        return parts.Length >= 3
            ? new PrinterInfo(parts[0], parts[1], parts[2])
            : null;
    }

    private static async Task<(bool Success, string Output, string Error)> RunPowerShellAsync(
        string script,
        CancellationToken cancellationToken)
    {
        var encodedScript = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedScript}",
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("No se pudo iniciar PowerShell.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode == 0, await outputTask, await errorTask);
    }
}
