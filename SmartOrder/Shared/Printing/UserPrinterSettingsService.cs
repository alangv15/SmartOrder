using Microsoft.Extensions.Configuration;
using SmartOrder.Shared.Services;
using System.Text.Json;

namespace SmartOrder.Shared.Printing;

public sealed class UserPrinterSettingsService : IUserPrinterSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly IConfiguration _configuration;

    public UserPrinterSettingsService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string SettingsFilePath
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartOrder");
            return Path.Combine(folder, "printer-settings.json");
        }
    }

    public async Task<TicketPrinterSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await LoadUserSettingsAsync(cancellationToken) ?? new TicketPrinterSettings();
        ApplyAppSettingsFallback(settings);
        return settings;
    }

    public async Task SaveAsync(TicketPrinterSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsFilePath)!);
        await using var stream = File.Create(SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }

    private async Task<TicketPrinterSettings?> LoadUserSettingsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SettingsFilePath))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(SettingsFilePath);
            return await JsonSerializer.DeserializeAsync<TicketPrinterSettings>(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("UserPrinterSettingsService.LoadUserSettingsAsync", ex, SettingsFilePath);
            return null;
        }
    }

    private void ApplyAppSettingsFallback(TicketPrinterSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ConnectionType))
        {
            settings.ConnectionType = _configuration["PrinterSettings:TicketConnectionType"] ?? TicketConnectionTypes.Serial;
        }

        if (string.IsNullOrWhiteSpace(settings.TicketPortName))
        {
            settings.TicketPortName = _configuration["PrinterSettings:TicketPortName"];
        }

        if (string.IsNullOrWhiteSpace(settings.TicketPrinterName))
        {
            settings.TicketPrinterName = _configuration["PrinterSettings:TicketPrinterName"];
        }

        if (string.IsNullOrWhiteSpace(settings.TicketLogoAsset))
        {
            settings.TicketLogoAsset = _configuration["PrinterSettings:TicketLogoAsset"] ?? "LogoMiga.escpos";
        }
    }
}

