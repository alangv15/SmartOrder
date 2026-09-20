namespace SmartOrder.Shared.Printing;

public interface IWindowsPrinterDiscoveryService
{
    Task<IReadOnlyList<string>> GetSerialPortsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrinterInfo>> GetPrintersAsync(CancellationToken cancellationToken = default);
    Task<PrinterConfigurationResult> ConfigureDefaultUsbPrinterAsync(CancellationToken cancellationToken = default);
}

