namespace SmartOrder.Shared.Printing;

public interface ITicketPortWriter
{
    Task WriteAsync(string portName, byte[] payload, CancellationToken cancellationToken = default);
}
