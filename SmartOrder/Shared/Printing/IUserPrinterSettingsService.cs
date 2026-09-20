namespace SmartOrder.Shared.Printing;

public interface IUserPrinterSettingsService
{
    string SettingsFilePath { get; }
    Task<TicketPrinterSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TicketPrinterSettings settings, CancellationToken cancellationToken = default);
}

