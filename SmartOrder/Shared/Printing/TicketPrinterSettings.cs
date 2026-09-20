namespace SmartOrder.Shared.Printing;

public static class TicketConnectionTypes
{
    public const string Serial = "Serial";
    public const string WindowsPrinter = "WindowsPrinter";
}

public sealed class TicketPrinterSettings
{
    public string ConnectionType { get; set; } = TicketConnectionTypes.Serial;
    public string? TicketPortName { get; set; }
    public string? TicketPrinterName { get; set; }
    public string? TicketLogoAsset { get; set; } = "LogoMiga.escpos";
}

public sealed record PrinterInfo(string Name, string PortName, string DriverName);

public sealed record PrinterConfigurationResult(bool Success, string Message, string? PrinterName = null, string? PortName = null);

