using SmartOrder.Business.Orders.Services;
using SmartOrder.Entities.Orders.Models;
using SmartOrder.Shared.Catalogs;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace SmartOrder.Modules.Orders.Pages;

public partial class OrderListPage : ContentPage, INotifyPropertyChanged
{
    private readonly OrderService _orderService;
    private readonly OrderStatusService _orderStatusService;
    private readonly PaymentStatusService _paymentStatusService;
    private bool _isLoading;
    private bool _isApplyingStatusUpdate;

    public OrderListPage(
        OrderService orderService,
        OrderStatusService orderStatusService,
        PaymentStatusService paymentStatusService)
    {
        InitializeComponent();
        _orderService = orderService;
        _orderStatusService = orderStatusService;
        _paymentStatusService = paymentStatusService;
        StartDate = DateTime.Today.AddDays(-15);
        EndDate = DateTime.Today;
        RefreshCommand = new Command(async () => await LoadOrdersAsync());
        BindingContext = this;
    }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string TotalRecordsText => Orders.Count.ToString("N0");
    public ICommand RefreshCommand { get; }
    public ObservableCollection<OrderListItemViewModel> Orders { get; } = new();
    public ObservableCollection<OrderStatusDto> OrderStatuses { get; } = new();
    public ObservableCollection<PaymentStatusDto> PaymentStatuses { get; } = new();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCatalogsAsync();
        await LoadOrdersAsync();
    }

    private async Task LoadCatalogsAsync()
    {
        if (OrderStatuses.Any() && PaymentStatuses.Any())
        {
            return;
        }

        try
        {
            _isLoading = true;
            var orderStatuses = await _orderStatusService.GetAllAsync();
            var paymentStatuses = await _paymentStatusService.GetAllAsync();

            OrderStatuses.Clear();
            foreach (var status in orderStatuses.Where(status => status.IsActive))
            {
                status.DisplayName = string.IsNullOrWhiteSpace(status.DisplayName)
                    ? status.OrderStatusCode
                    : status.DisplayName.Trim();
                OrderStatuses.Add(status);
            }

            PaymentStatuses.Clear();
            foreach (var status in paymentStatuses.Where(status => status.IsActive))
            {
                status.DisplayName = string.IsNullOrWhiteSpace(status.DisplayName)
                    ? status.PaymentStatusCode
                    : status.DisplayName.Trim();
                PaymentStatuses.Add(status);
            }
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("OrderListPage.LoadCatalogsAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los catalogos",
                Message = "Revisa la conexion con el API e intenta de nuevo."
            });
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadOrdersAsync()
    {
        try
        {
            _isLoading = true;
            Orders.Clear();
            OnPropertyChanged(nameof(TotalRecordsText));

            var (startDate, endDate) = NormalizeDateRange(StartDate, EndDate);
            StartDate = startDate;
            EndDate = endDate;
            OnPropertyChanged(nameof(StartDate));
            OnPropertyChanged(nameof(EndDate));

            var startUtc = ToUtcFromLocalDate(startDate);
            var endExclusiveUtc = ToUtcFromLocalDate(endDate.AddDays(1));
            var summaries = await _orderService.GetCustomOrderSummariesAsync(startUtc, endExclusiveUtc);

            foreach (var summary in summaries)
            {
                Orders.Add(new OrderListItemViewModel(summary, OrderStatuses, PaymentStatuses));
            }
            OnPropertyChanged(nameof(TotalRecordsText));
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("OrderListPage.LoadOrdersAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los pedidos",
                Message = "Ocurrio un error al consultar el rango seleccionado."
            });
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void OnNewOrderClicked(object sender, EventArgs e)
    {
        await Microsoft.Maui.Controls.Shell.Current.GoToAsync("//OrderFormPage?mode=new");
    }

    private async void OnEditOrderClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is OrderListItemViewModel item)
        {
            await Microsoft.Maui.Controls.Shell.Current.GoToAsync($"//OrderFormPage?orderId={item.OrderId}");
        }
    }

    private async void OnStatusPickerChanged(object sender, EventArgs e)
    {
        if (_isLoading || _isApplyingStatusUpdate)
        {
            return;
        }

        if (sender is not Picker picker || picker.BindingContext is not OrderListItemViewModel item)
        {
            return;
        }

        if (item.SelectedOrderStatus == null || item.SelectedPaymentStatus == null || !item.HasStatusChanged)
        {
            return;
        }

        try
        {
            _isApplyingStatusUpdate = true;
            var updated = await _orderService.UpdateStatusAsync(item.OrderId, new OrderStatusUpdateDto
            {
                OrderStatusCode = item.SelectedOrderStatus.OrderStatusCode,
                PaymentStatusCode = item.SelectedPaymentStatus.PaymentStatusCode
            });

            if (!updated)
            {
                item.RestoreOriginalStatus();
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se actualizo el pedido",
                    Message = $"No se pudo guardar el estatus del folio #{item.OrderId}."
                });
                return;
            }

            item.AcceptCurrentStatus();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("OrderListPage.OnStatusPickerChanged", ex);
            item.RestoreOriginalStatus();
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al actualizar estatus",
                Message = $"No se pudo guardar el cambio del folio #{item.OrderId}."
            });
        }
        finally
        {
            _isApplyingStatusUpdate = false;
        }
    }

    private static (DateTime StartDate, DateTime EndDate) NormalizeDateRange(DateTime startDate, DateTime endDate)
    {
        startDate = startDate.Date;
        endDate = endDate.Date;
        return endDate < startDate
            ? (endDate, startDate)
            : (startDate, endDate);
    }

    private static DateTime ToUtcFromLocalDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Local).ToUniversalTime();
    }

    private static DateTime ToLocalDisplayDateTime(DateTime? utcDateTime)
    {
        if (!utcDateTime.HasValue)
        {
            return DateTime.MinValue;
        }

        var value = utcDateTime.Value.Kind == DateTimeKind.Utc
            ? utcDateTime.Value
            : DateTime.SpecifyKind(utcDateTime.Value, DateTimeKind.Utc);
        return value.ToLocalTime();
    }

    public sealed class OrderListItemViewModel : INotifyPropertyChanged
    {
        private readonly IReadOnlyList<OrderStatusDto> _orderStatuses;
        private readonly IReadOnlyList<PaymentStatusDto> _paymentStatuses;
        private OrderStatusDto? _selectedOrderStatus;
        private PaymentStatusDto? _selectedPaymentStatus;
        private string _originalOrderStatusCode;
        private string _originalPaymentStatusCode;

        public OrderListItemViewModel(
            OrderSummaryDto summary,
            IReadOnlyList<OrderStatusDto> orderStatuses,
            IReadOnlyList<PaymentStatusDto> paymentStatuses)
        {
            _orderStatuses = orderStatuses;
            _paymentStatuses = paymentStatuses;
            OrderId = summary.OrderId;
            IsInternalProduction = summary.IsInternalProduction;
            CustomerName = IsInternalProduction
                ? "Produccion del local"
                : NormalizeText(summary.CustomerName, "Cliente no encontrado");
            Pieces = summary.Pieces;
            ProductSummary = NormalizeText(summary.ProductSummary, "Sin productos");
            CommentsSummary = NormalizeText(summary.Comments, "Sin comentarios");
            ProductionStartText = FormatDate(summary.ProductionStartDate);
            ProductionEndText = FormatDate(summary.ProductionEndDate);
            var deliveryDateTime = ToLocalDisplayDateTime(summary.DeliveryDate);
            DeliveryDateText = deliveryDateTime == DateTime.MinValue ? "Sin fecha" : deliveryDateTime.ToString("dd/MM/yyyy");
            DeliveryTimeText = deliveryDateTime == DateTime.MinValue ? "--:--" : deliveryDateTime.ToString("HH:mm");

            _originalOrderStatusCode = summary.OrderStatusCode;
            _originalPaymentStatusCode = summary.PaymentStatusCode;
            _selectedOrderStatus = FindOrderStatus(_originalOrderStatusCode);
            _selectedPaymentStatus = FindPaymentStatus(_originalPaymentStatusCode);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int OrderId { get; }
        public bool IsInternalProduction { get; }
        public string CustomerName { get; }
        public string OrderTypeText => IsInternalProduction ? "Produccion del local" : "Pedido de cliente";
        public int Pieces { get; }
        public string ProductSummary { get; }
        public string CommentsSummary { get; }
        public string ProductionStartText { get; }
        public string ProductionEndText { get; }
        public string DeliveryDateText { get; }
        public string DeliveryTimeText { get; }

        public OrderStatusDto? SelectedOrderStatus
        {
            get => _selectedOrderStatus;
            set
            {
                if (_selectedOrderStatus != value)
                {
                    _selectedOrderStatus = value;
                    OnPropertyChanged(nameof(SelectedOrderStatus));
                    OnPropertyChanged(nameof(HasStatusChanged));
                }
            }
        }

        public PaymentStatusDto? SelectedPaymentStatus
        {
            get => _selectedPaymentStatus;
            set
            {
                if (_selectedPaymentStatus != value)
                {
                    _selectedPaymentStatus = value;
                    OnPropertyChanged(nameof(SelectedPaymentStatus));
                    OnPropertyChanged(nameof(HasStatusChanged));
                }
            }
        }

        public bool HasStatusChanged =>
            !CodesEqual(SelectedOrderStatus?.OrderStatusCode, _originalOrderStatusCode)
            || !CodesEqual(SelectedPaymentStatus?.PaymentStatusCode, _originalPaymentStatusCode);

        public void AcceptCurrentStatus()
        {
            _originalOrderStatusCode = SelectedOrderStatus?.OrderStatusCode ?? _originalOrderStatusCode;
            _originalPaymentStatusCode = SelectedPaymentStatus?.PaymentStatusCode ?? _originalPaymentStatusCode;
            OnPropertyChanged(nameof(HasStatusChanged));
        }

        public void RestoreOriginalStatus()
        {
            SelectedOrderStatus = FindOrderStatus(_originalOrderStatusCode);
            SelectedPaymentStatus = FindPaymentStatus(_originalPaymentStatusCode);
        }

        private OrderStatusDto? FindOrderStatus(string? code)
        {
            return _orderStatuses.FirstOrDefault(status => CodesEqual(status.OrderStatusCode, code));
        }

        private PaymentStatusDto? FindPaymentStatus(string? code)
        {
            return _paymentStatuses.FirstOrDefault(status => CodesEqual(status.PaymentStatusCode, code));
        }

        private static string FormatDate(DateTime? utcDateTime)
        {
            var date = ToLocalDisplayDateTime(utcDateTime);
            return date == DateTime.MinValue ? "Sin fecha" : date.ToString("dd/MM/yyyy");
        }

        private static string NormalizeText(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static bool CodesEqual(string? left, string? right)
        {
            return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
