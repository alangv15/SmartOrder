using Microsoft.Maui.Storage;
using SmartOrder.Shared.Services;

namespace SmartOrder.Shared.Printing;

public sealed class TicketPrinterService : ITicketPrinterService
{
    private static readonly TimeSpan DefaultPrintTimeout = TimeSpan.FromSeconds(5);

    private readonly EscPosTicketFormatter _formatter;
    private readonly ITicketPortWriter _portWriter;
    private readonly WindowsPrinterTicketWriter _windowsPrinterWriter;
    private readonly IUserPrinterSettingsService _settingsService;
    private readonly SemaphoreSlim _printLock = new(1, 1);

    public TicketPrinterService(
        EscPosTicketFormatter formatter,
        ITicketPortWriter portWriter,
        WindowsPrinterTicketWriter windowsPrinterWriter,
        IUserPrinterSettingsService settingsService)
    {
        _formatter = formatter;
        _portWriter = portWriter;
        _windowsPrinterWriter = windowsPrinterWriter;
        _settingsService = settingsService;
    }

    public async Task<TicketPrintResult> PrintAsync(
        TicketDocument document,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveTimeout = timeout.GetValueOrDefault(DefaultPrintTimeout);
        if (effectiveTimeout <= TimeSpan.Zero)
        {
            effectiveTimeout = DefaultPrintTimeout;
        }

        if (!await _printLock.WaitAsync(0, cancellationToken))
        {
            return TicketPrintResult.Error(
                "Ya hay una impresion en proceso. Intenta reimprimir el ticket en unos segundos.");
        }

        var printTask = Task.Run(async () =>
        {
            try
            {
                return await PrintCoreAsync(document, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return TicketPrintResult.Error("La impresion fue cancelada.");
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("TicketPrinterService.PrintAsync", ex);
                return TicketPrintResult.Error(
                    "Ocurrio un error inesperado al preparar la impresion del ticket.");
            }
            finally
            {
                _printLock.Release();
            }
        }, CancellationToken.None);

        var completedTask = await Task.WhenAny(printTask, Task.Delay(effectiveTimeout, CancellationToken.None));
        if (completedTask == printTask)
        {
            return await printTask;
        }

        FileErrorLogger.LogMessage(
            "TicketPrinterService.PrintAsync",
            $"Timeout despues de {effectiveTimeout.TotalSeconds:0.#} segundos.");

        return TicketPrintResult.Error(
            $"La impresora no respondio en {effectiveTimeout.TotalSeconds:0.#} segundos. Puedes reintentar la impresion desde la opcion de ticket.");
    }

    private async Task<TicketPrintResult> PrintCoreAsync(TicketDocument document, CancellationToken cancellationToken)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);
        if (string.Equals(settings.ConnectionType, TicketConnectionTypes.WindowsPrinter, StringComparison.OrdinalIgnoreCase))
        {
            return await PrintToWindowsPrinterAsync(document, settings, cancellationToken);
        }

        return await PrintToSerialPortAsync(document, settings, cancellationToken);
    }

    private async Task<TicketPrintResult> PrintToSerialPortAsync(
        TicketDocument document,
        TicketPrinterSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TicketPortName))
        {
            return TicketPrintResult.NotConfigured(
                "Configura la impresora desde Configuracion > Impresora o selecciona un puerto COM.");
        }

        try
        {
            var logoBytes = await LoadLogoAsync(settings.TicketLogoAsset, cancellationToken);
            var payload = _formatter.Format(document, logoBytes);
            await _portWriter.WriteAsync(settings.TicketPortName, payload, cancellationToken);
            return TicketPrintResult.Ok("El ticket se envio correctamente a la impresora.");
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("TicketPrinterService.PrintToSerialPortAsync", ex, $"Port: {settings.TicketPortName}");

            return TicketPrintResult.Error(
                $"No se pudo escribir en el puerto {settings.TicketPortName}. Verifica que la impresora este encendida y que otra aplicacion no este usando el puerto.");
        }
    }

    private async Task<TicketPrintResult> PrintToWindowsPrinterAsync(
        TicketDocument document,
        TicketPrinterSettings settings,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(settings.TicketPrinterName))
        {
            return TicketPrintResult.NotConfigured(
                "Configura la impresora desde Configuracion > Impresora o selecciona una impresora de Windows.");
        }

        try
        {
            var logoBytes = await LoadLogoAsync(settings.TicketLogoAsset, cancellationToken);
            var payload = _formatter.Format(document, logoBytes);
            await _windowsPrinterWriter.WriteAsync(settings.TicketPrinterName, payload, cancellationToken);
            return TicketPrintResult.Ok("El ticket se envio correctamente a la impresora.");
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("TicketPrinterService.PrintToWindowsPrinterAsync", ex, $"Printer: {settings.TicketPrinterName}");

            return TicketPrintResult.Error(
                $"No se pudo escribir en la impresora {settings.TicketPrinterName}. Verifica que este encendida, conectada por USB y disponible en Windows.");
        }
    }

    private async Task<byte[]?> LoadLogoAsync(string? ticketLogoAsset, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ticketLogoAsset))
        {
            return null;
        }

        await using var stream = await FileSystem.OpenAppPackageFileAsync(ticketLogoAsset);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }
}
