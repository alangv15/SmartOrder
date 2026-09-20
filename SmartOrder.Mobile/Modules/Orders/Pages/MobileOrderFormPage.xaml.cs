using SmartOrder.Mobile.Modules.Orders.ViewModels;

namespace SmartOrder.Mobile.Modules.Orders.Pages;

public partial class MobileOrderFormPage : ContentPage, IQueryAttributable
{
    private readonly MobileOrderFormViewModel _viewModel;
    private int? _orderId;
    private bool _isInitialized;

    public MobileOrderFormPage(MobileOrderFormViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _isInitialized = false;
        _orderId = null;
        if (query.TryGetValue("orderId", out var value) && int.TryParse(value?.ToString(), out var parsedOrderId))
        {
            _orderId = parsedOrderId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await _viewModel.InitializeAsync(_orderId);
    }
}
