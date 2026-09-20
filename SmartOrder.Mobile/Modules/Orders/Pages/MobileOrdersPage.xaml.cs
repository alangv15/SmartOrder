using SmartOrder.Mobile.Modules.Orders.ViewModels;

namespace SmartOrder.Mobile.Modules.Orders.Pages;

public partial class MobileOrdersPage : ContentPage
{
    private readonly MobileOrdersViewModel _viewModel;

    public MobileOrdersPage(MobileOrdersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnEditOrderClicked(object sender, EventArgs e)
    {
        if (sender is Button { BindingContext: MobileOrderListItemViewModel item })
        {
            await _viewModel.EditAsync(item);
        }
    }

    private async void OnStatusPickerChanged(object sender, EventArgs e)
    {
        if (sender is Picker { BindingContext: MobileOrderListItemViewModel item })
        {
            await _viewModel.ApplyStatusUpdateAsync(item);
        }
    }
}
