using SmartOrder.Business.Orders.Services;
using SmartOrder.Entities.Orders.Models;
using SmartOrder.Mobile.Shared;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SmartOrder.Mobile.Modules.Orders.ViewModels;

public sealed class MobileOrdersViewModel : ObservableObject
{
    private readonly OrderService _orderService;
    private readonly OrderStatusService _orderStatusService;
    private readonly PaymentStatusService _paymentStatusService;
    private bool _isLoading;
    private bool _isApplyingStatusUpdate;
    private DateTime _startDate = DateTime.Today.AddDays(-15);
    private DateTime _endDate = DateTime.Today;
    private string _message = string.Empty;

    public MobileOrdersViewModel(
        OrderService orderService,
        OrderStatusService orderStatusService,
        PaymentStatusService paymentStatusService)
    {
        _orderService = orderService;
        _orderStatusService = orderStatusService;
        _paymentStatusService = paymentStatusService;
        RefreshCommand = new Command(async () => await LoadAsync(), () => !IsLoading);
        NewOrderCommand = new Command(async () => await Shell.Current.GoToAsync($"{nameof(Pages.MobileOrderFormPage)}?mode=new"));
    }

    public ObservableCollection<MobileOrderListItemViewModel> Orders { get; } = new();
    public ObservableCollection<OrderStatusDto> OrderStatuses { get; } = new();
    public ObservableCollection<PaymentStatusDto> PaymentStatuses { get; } = new();
    public ICommand RefreshCommand { get; }
    public ICommand NewOrderCommand { get; }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value) && RefreshCommand is Command command)
            {
                command.ChangeCanExecute();
            }
        }
    }

    public string Message
    {
        get => _message;
        private set => SetProperty(ref _message, value);
    }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasOrders => Orders.Count > 0;
    public string TotalRecordsText => $"{Orders.Count:N0} pedidos";

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            Message = string.Empty;
            OnPropertyChanged(nameof(HasMessage));
            await LoadCatalogsAsync();
            Orders.Clear();

            var startDate = StartDate.Date;
            var endDate = EndDate.Date;
            if (endDate < startDate)
            {
                (startDate, endDate) = (endDate, startDate);
                StartDate = startDate;
                EndDate = endDate;
            }

            var summaries = await _orderService.GetCustomOrderSummariesAsync(
                ToUtcFromLocalDate(startDate),
                ToUtcFromLocalDate(endDate.AddDays(1)));

            foreach (var summary in summaries)
            {
                Orders.Add(new MobileOrderListItemViewModel(summary, OrderStatuses, PaymentStatuses));
            }

            OnPropertyChanged(nameof(HasOrders));
            OnPropertyChanged(nameof(TotalRecordsText));
        }
        catch
        {
            Message = "No fue posible cargar los pedidos. Revisa la conexión con el API.";
            OnPropertyChanged(nameof(HasMessage));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task EditAsync(MobileOrderListItemViewModel item)
    {
        await Shell.Current.GoToAsync($"{nameof(Pages.MobileOrderFormPage)}?orderId={item.OrderId}");
    }

    public async Task ApplyStatusUpdateAsync(MobileOrderListItemViewModel item)
    {
        if (_isApplyingStatusUpdate || item.SelectedOrderStatus == null || item.SelectedPaymentStatus == null || !item.HasStatusChanged)
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

            if (updated)
            {
                item.AcceptCurrentStatus();
                return;
            }

            item.RestoreOriginalStatus();
            Message = $"No se pudo actualizar el folio #{item.OrderId}.";
            OnPropertyChanged(nameof(HasMessage));
        }
        catch
        {
            item.RestoreOriginalStatus();
            Message = $"No se pudo actualizar el folio #{item.OrderId}.";
            OnPropertyChanged(nameof(HasMessage));
        }
        finally
        {
            _isApplyingStatusUpdate = false;
        }
    }

    private async Task LoadCatalogsAsync()
    {
        if (OrderStatuses.Count > 0 && PaymentStatuses.Count > 0)
        {
            return;
        }

        OrderStatuses.Clear();
        foreach (var status in (await _orderStatusService.GetAllAsync()).Where(status => status.IsActive))
        {
            status.DisplayName = string.IsNullOrWhiteSpace(status.DisplayName) ? status.OrderStatusCode : status.DisplayName.Trim();
            OrderStatuses.Add(status);
        }

        PaymentStatuses.Clear();
        foreach (var status in (await _paymentStatusService.GetAllAsync()).Where(status => status.IsActive))
        {
            status.DisplayName = string.IsNullOrWhiteSpace(status.DisplayName) ? status.PaymentStatusCode : status.DisplayName.Trim();
            PaymentStatuses.Add(status);
        }
    }

    private static DateTime ToUtcFromLocalDate(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Local).ToUniversalTime();
    }
}
