using SmartOrder.Shared.Printing;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class PrinterSettingsPage : ContentPage
{
    private readonly IUserPrinterSettingsService _settingsService;
    private readonly IWindowsPrinterDiscoveryService _discoveryService;
    private readonly ITicketPrinterService _ticketPrinterService;
    private PrinterConnectionOption? _selectedConnectionType;
    private string? _selectedComPort;
    private PrinterInfo? _selectedPrinter;
    private string _statusText = "Cargando configuración...";
    private string _currentConnectionText = "Sin cargar";
    private string _currentDeviceText = "Sin cargar";
    private bool _hasLoaded;

    public PrinterSettingsPage(
        IUserPrinterSettingsService settingsService,
        IWindowsPrinterDiscoveryService discoveryService,
        ITicketPrinterService ticketPrinterService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        _discoveryService = discoveryService;
        _ticketPrinterService = ticketPrinterService;

        ConnectionOptions.Add(new PrinterConnectionOption("Windows / USB", TicketConnectionTypes.WindowsPrinter));
        ConnectionOptions.Add(new PrinterConnectionOption("Bluetooth / Puerto COM", TicketConnectionTypes.Serial));
        BindingContext = this;
    }

    public ObservableCollection<PrinterConnectionOption> ConnectionOptions { get; } = new();
    public ObservableCollection<string> AvailableComPorts { get; } = new();
    public ObservableCollection<PrinterInfo> AvailablePrinters { get; } = new();

    public string SettingsFilePath => $"Archivo local: {_settingsService.SettingsFilePath}";
    public bool IsSerialSelected => SelectedConnectionType?.Code == TicketConnectionTypes.Serial;
    public bool IsWindowsPrinterSelected => SelectedConnectionType?.Code == TicketConnectionTypes.WindowsPrinter;

    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string CurrentConnectionText
    {
        get => _currentConnectionText;
        set
        {
            if (_currentConnectionText != value)
            {
                _currentConnectionText = value;
                OnPropertyChanged(nameof(CurrentConnectionText));
            }
        }
    }

    public string CurrentDeviceText
    {
        get => _currentDeviceText;
        set
        {
            if (_currentDeviceText != value)
            {
                _currentDeviceText = value;
                OnPropertyChanged(nameof(CurrentDeviceText));
            }
        }
    }

    public PrinterConnectionOption? SelectedConnectionType
    {
        get => _selectedConnectionType;
        set
        {
            if (_selectedConnectionType != value)
            {
                _selectedConnectionType = value;
                OnPropertyChanged(nameof(SelectedConnectionType));
                OnPropertyChanged(nameof(IsSerialSelected));
                OnPropertyChanged(nameof(IsWindowsPrinterSelected));
            }
        }
    }

    public string? SelectedComPort
    {
        get => _selectedComPort;
        set
        {
            if (_selectedComPort != value)
            {
                _selectedComPort = value;
                OnPropertyChanged(nameof(SelectedComPort));
            }
        }
    }

    public PrinterInfo? SelectedPrinter
    {
        get => _selectedPrinter;
        set
        {
            if (_selectedPrinter != value)
            {
                _selectedPrinter = value;
                OnPropertyChanged(nameof(SelectedPrinter));
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_hasLoaded)
        {
            return;
        }

        _hasLoaded = true;
        await LoadAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await RefreshAvailableDevicesAsync();
    }

    private async void OnConfigureUsbClicked(object sender, EventArgs e)
    {
        try
        {
            StatusText = "Configurando impresora USB...";
            var result = await _discoveryService.ConfigureDefaultUsbPrinterAsync();

            if (result.Success)
            {
                await RefreshAvailableDevicesAsync(result.PrinterName);
                SelectedConnectionType = ConnectionOptions.First(option => option.Code == TicketConnectionTypes.WindowsPrinter);
                SelectedPrinter = AvailablePrinters.FirstOrDefault(printer =>
                    string.Equals(printer.Name, result.PrinterName, StringComparison.OrdinalIgnoreCase));
                await SaveCurrentSelectionAsync();
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = result.Success ? AppMessageType.Success : AppMessageType.Error,
                Title = result.Success ? "Impresora configurada" : "No se pudo configurar",
                Message = result.Message
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("PrinterSettingsPage.OnConfigureUsbClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo configurar",
                Message = "No fue posible configurar la impresora USB desde la aplicación."
            });
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (await SaveCurrentSelectionAsync())
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Success,
                Title = "Configuración guardada",
                Message = "La impresora quedó guardada para este equipo."
            });
        }
    }

    private async void OnTestClicked(object sender, EventArgs e)
    {
        if (!await SaveCurrentSelectionAsync())
        {
            return;
        }

        var result = await _ticketPrinterService.PrintAsync(BuildTestTicket());
        await AppMessageService.ShowAsync(new AppMessageOptions
        {
            Type = result.Success ? AppMessageType.Success : AppMessageType.Info,
            Title = result.Title,
            Message = result.Message
        });
    }

    private async Task LoadAsync()
    {
        try
        {
            StatusText = "Consultando impresoras disponibles...";
            var settings = await _settingsService.LoadAsync();
            await LoadAvailableDevicesAsync();

            SelectedConnectionType = ConnectionOptions.FirstOrDefault(option =>
                    string.Equals(option.Code, settings.ConnectionType, StringComparison.OrdinalIgnoreCase))
                ?? ConnectionOptions.First();
            SelectedComPort = AvailableComPorts.FirstOrDefault(port =>
                    string.Equals(port, settings.TicketPortName, StringComparison.OrdinalIgnoreCase))
                ?? AvailableComPorts.FirstOrDefault();
            SelectedPrinter = AvailablePrinters.FirstOrDefault(printer =>
                    string.Equals(printer.Name, settings.TicketPrinterName, StringComparison.OrdinalIgnoreCase))
                ?? AvailablePrinters.FirstOrDefault(printer =>
                    string.Equals(printer.Name, WindowsPrinterDiscoveryService.DefaultUsbPrinterName, StringComparison.OrdinalIgnoreCase))
                ?? AvailablePrinters.FirstOrDefault();

            UpdateCurrentSettingsText(settings);
            StatusText = "Configuración cargada.";
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("PrinterSettingsPage.LoadAsync", ex);
            StatusText = "No se pudo consultar la configuración.";
        }
    }

    private async Task RefreshAvailableDevicesAsync(string? preferredPrinterName = null)
    {
        var selectedPort = SelectedComPort;
        var selectedPrinterName = preferredPrinterName ?? SelectedPrinter?.Name;

        StatusText = "Actualizando dispositivos disponibles...";
        await LoadAvailableDevicesAsync();

        SelectedComPort = AvailableComPorts.FirstOrDefault(port =>
                string.Equals(port, selectedPort, StringComparison.OrdinalIgnoreCase))
            ?? AvailableComPorts.FirstOrDefault();
        SelectedPrinter = AvailablePrinters.FirstOrDefault(printer =>
                string.Equals(printer.Name, selectedPrinterName, StringComparison.OrdinalIgnoreCase))
            ?? AvailablePrinters.FirstOrDefault();

        var currentSettings = await _settingsService.LoadAsync();
        UpdateCurrentSettingsText(currentSettings);
        StatusText = "Dispositivos actualizados.";
    }

    private async Task LoadAvailableDevicesAsync()
    {
        AvailableComPorts.Clear();
        foreach (var port in await _discoveryService.GetSerialPortsAsync())
        {
            AvailableComPorts.Add(port);
        }

        AvailablePrinters.Clear();
        foreach (var printer in await _discoveryService.GetPrintersAsync())
        {
            AvailablePrinters.Add(printer);
        }
    }

    private async Task<bool> SaveCurrentSelectionAsync()
    {
        var connectionType = SelectedConnectionType?.Code ?? TicketConnectionTypes.Serial;
        if (connectionType == TicketConnectionTypes.Serial && string.IsNullOrWhiteSpace(SelectedComPort))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Falta puerto COM",
                Message = "Selecciona un puerto COM antes de guardar."
            });
            return false;
        }

        if (connectionType == TicketConnectionTypes.WindowsPrinter && string.IsNullOrWhiteSpace(SelectedPrinter?.Name))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Falta impresora",
                Message = "Selecciona una impresora Windows antes de guardar."
            });
            return false;
        }

        await _settingsService.SaveAsync(new TicketPrinterSettings
        {
            ConnectionType = connectionType,
            TicketPortName = SelectedComPort,
            TicketPrinterName = SelectedPrinter?.Name,
            TicketLogoAsset = "LogoMiga.escpos"
        });

        UpdateCurrentSettingsText(new TicketPrinterSettings
        {
            ConnectionType = connectionType,
            TicketPortName = SelectedComPort,
            TicketPrinterName = SelectedPrinter?.Name,
            TicketLogoAsset = "LogoMiga.escpos"
        });

        StatusText = "Configuración guardada. La prueba usará estos valores.";
        return true;
    }

    private static TicketDocument BuildTestTicket()
    {
        return new TicketDocument(
            "Miga",
            new[] { "Pan artesanal", "Prueba de impresion" },
            new[]
            {
                new TicketInfoLine("Fecha", DateTime.Now.ToString("dd/MM/yyyy HH:mm")),
                new TicketInfoLine("Origen", "Configuracion")
            },
            new[]
            {
                new TicketItemLine("Prueba", "Ticket SmartOrder", 1, 1, 0, 1)
            },
            false,
            new[]
            {
                new TicketTotalLine("Total", 1, IsGrandTotal: true)
            },
            new[] { "Impresora configurada correctamente" });
    }

    private void UpdateCurrentSettingsText(TicketPrinterSettings settings)
    {
        if (string.Equals(settings.ConnectionType, TicketConnectionTypes.WindowsPrinter, StringComparison.OrdinalIgnoreCase))
        {
            CurrentConnectionText = "Windows / USB";
            CurrentDeviceText = settings.TicketPrinterName ?? "Sin impresora seleccionada";
            return;
        }

        CurrentConnectionText = "Bluetooth / Puerto COM";
        CurrentDeviceText = settings.TicketPortName ?? "Sin puerto seleccionado";
    }

}

public sealed record PrinterConnectionOption(string Label, string Code);
