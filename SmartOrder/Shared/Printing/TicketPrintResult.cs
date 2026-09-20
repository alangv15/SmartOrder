namespace SmartOrder.Shared.Printing;

public sealed record TicketPrintResult(bool Success, string Title, string Message)
{
    public static TicketPrintResult Ok(string message)
    {
        return new TicketPrintResult(true, "Ticket impreso", message);
    }

    public static TicketPrintResult NotConfigured(string message)
    {
        return new TicketPrintResult(false, "Impresora no configurada", message);
    }

    public static TicketPrintResult Error(string message)
    {
        return new TicketPrintResult(false, "No se pudo imprimir", message);
    }
}
