namespace SmartOrder.Shared.Printing;

public interface ITicketPrinterService
{
    Task<TicketPrintResult> PrintAsync(
        TicketDocument document,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
