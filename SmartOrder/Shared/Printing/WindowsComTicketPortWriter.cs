namespace SmartOrder.Shared.Printing;

public sealed class WindowsComTicketPortWriter : ITicketPortWriter
{
    public async Task WriteAsync(string portName, byte[] payload, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portName))
        {
            throw new InvalidOperationException("No se configuro el puerto de la impresora.");
        }

        var normalizedPortName = NormalizePortName(portName);
        await using var stream = new FileStream(
            normalizedPortName,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static string NormalizePortName(string portName)
    {
        var normalized = portName.Trim();
        return normalized.StartsWith(@"\\.\", StringComparison.Ordinal)
            ? normalized
            : $@"\\.\{normalized}";
    }
}
