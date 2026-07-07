using Microsoft.Extensions.Configuration;
using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Modules.Orders.ViewModels;

namespace SmartOrder.Modules.Orders.Pages;

public partial class OrderFormPage : ContentPage, IQueryAttributable
{
    private readonly OrderFormViewModel _viewModel;
    private int? _pendingOrderId;
    private int? _loadedOrderId;
    private bool _pendingNewOrder;
    private bool _isNewModeActive;

    public OrderFormPage(
        IConfiguration configuration,
        OrderService orderService,
        ProductService productService,
        CategoryService categoryService,
        CustomerService customerService,
        BranchService branchService,
        OrderStatusService orderStatusService,
        PaymentStatusService paymentStatusService,
        DiscountRuleService discountRuleService,
        DiscountLimitRuleService discountLimitRuleService)
    {
        InitializeComponent();
        var defaultUserId = configuration.GetValue<int?>("OperationSettings:DefaultUserId") ?? 1;
        _viewModel = new OrderFormViewModel(
            defaultUserId,
            orderService,
            productService,
            categoryService,
            customerService,
            branchService,
            orderStatusService,
            paymentStatusService,
            discountRuleService,
            discountLimitRuleService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.TryConsumeCatalogRefreshSkip())
        {
            await _viewModel.RefreshCatalogDataAsync();
        }

        await ApplyPendingNavigationAsync();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("mode", out var modeValue)
            && string.Equals(modeValue?.ToString(), "new", StringComparison.OrdinalIgnoreCase))
        {
            if (_isNewModeActive)
            {
                return;
            }

            _pendingNewOrder = true;
            _pendingOrderId = null;
            _loadedOrderId = null;
        }

        if (query.TryGetValue("orderId", out var orderIdValue)
            && int.TryParse(orderIdValue?.ToString(), out var orderId)
            && orderId > 0)
        {
            if (_loadedOrderId == orderId || _pendingOrderId == orderId)
            {
                return;
            }

            _pendingOrderId = orderId;
            _pendingNewOrder = false;
            _isNewModeActive = false;
        }
    }

    private async Task ApplyPendingNavigationAsync()
    {
        if (_pendingNewOrder)
        {
            _pendingNewOrder = false;
            _viewModel.ResetForm();
            _loadedOrderId = null;
            _isNewModeActive = true;
            return;
        }

        if (_pendingOrderId.HasValue)
        {
            var orderId = _pendingOrderId.Value;
            _pendingOrderId = null;
            await _viewModel.LoadOrderByIdAsync(orderId, showLoadedMessage: true);
            _loadedOrderId = _viewModel.EditingOrderId == orderId ? orderId : null;
            _isNewModeActive = false;
        }
    }
}
