using SmartOrder.Entities.Orders.Models;
using SmartOrder.Mobile.Shared;

namespace SmartOrder.Mobile.Modules.Orders.ViewModels;

public sealed class MobileOrderListItemViewModel : ObservableObject
{
    private readonly IReadOnlyList<OrderStatusDto> _orderStatuses;
    private readonly IReadOnlyList<PaymentStatusDto> _paymentStatuses;
    private string _originalOrderStatusCode;
    private string _originalPaymentStatusCode;
    private OrderStatusDto? _selectedOrderStatus;
    private PaymentStatusDto? _selectedPaymentStatus;

    public MobileOrderListItemViewModel(
        OrderSummaryDto summary,
        IReadOnlyList<OrderStatusDto> orderStatuses,
        IReadOnlyList<PaymentStatusDto> paymentStatuses)
    {
        _orderStatuses = orderStatuses;
        _paymentStatuses = paymentStatuses;
        OrderId = summary.OrderId;
        IsInternalProduction = summary.IsInternalProduction;
        CustomerName = IsInternalProduction ? "Produccion del local" : Normalize(summary.CustomerName, "Cliente no encontrado");
        Pieces = summary.Pieces;
        ProductSummary = Normalize(summary.ProductSummary, "Sin productos");
        CommentsSummary = Normalize(summary.Comments, "Sin comentarios");
        ProductionStartText = FormatDate(summary.ProductionStartDate);
        ProductionEndText = FormatDate(summary.ProductionEndDate);

        var delivery = ToLocalDisplayDateTime(summary.DeliveryDate);
        DeliveryDateText = delivery == DateTime.MinValue ? "Sin fecha" : delivery.ToString("dd/MM/yyyy");
        DeliveryTimeText = delivery == DateTime.MinValue ? "--:--" : delivery.ToString("HH:mm");

        _originalOrderStatusCode = summary.OrderStatusCode;
        _originalPaymentStatusCode = summary.PaymentStatusCode;
        _selectedOrderStatus = FindOrderStatus(_originalOrderStatusCode);
        _selectedPaymentStatus = FindPaymentStatus(_originalPaymentStatusCode);
    }

    public int OrderId { get; }
    public string FolioText => $"Folio #{OrderId}";
    public bool IsInternalProduction { get; }
    public string OrderTypeText => IsInternalProduction ? "Produccion del local" : "Pedido de cliente";
    public string CustomerName { get; }
    public int Pieces { get; }
    public string PiecesText => $"{Pieces:N0} pzas";
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
            if (SetProperty(ref _selectedOrderStatus, value))
            {
                OnPropertyChanged(nameof(HasStatusChanged));
            }
        }
    }

    public PaymentStatusDto? SelectedPaymentStatus
    {
        get => _selectedPaymentStatus;
        set
        {
            if (SetProperty(ref _selectedPaymentStatus, value))
            {
                OnPropertyChanged(nameof(HasStatusChanged));
            }
        }
    }

    public bool HasStatusChanged =>
        !CodesEqual(SelectedOrderStatus?.OrderStatusCode, _originalOrderStatusCode) ||
        !CodesEqual(SelectedPaymentStatus?.PaymentStatusCode, _originalPaymentStatusCode);

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

    private static string Normalize(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool CodesEqual(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
