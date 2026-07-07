using Microsoft.Extensions.Configuration;
using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Modules.Sales.ViewModels;

namespace SmartOrder.Modules.Sales.Pages;

public partial class SaleFormPage : ContentPage
{
    private readonly SaleFormViewModel _viewModel;

    public SaleFormPage(IConfiguration configuration, OrderService orderService, ProductService productService, CategoryService categoryService, DiscountRuleService discountRuleService, DiscountLimitRuleService discountLimitRuleService)
    {
        InitializeComponent();
        var defaultUserId = configuration.GetValue<int?>("OperationSettings:DefaultUserId") ?? 1;
        _viewModel = new SaleFormViewModel(defaultUserId, orderService, productService, categoryService, discountRuleService, discountLimitRuleService);
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_viewModel.TryConsumeCatalogRefreshSkip())
        {
            await _viewModel.RefreshCatalogDataAsync();
        }
    }
}
