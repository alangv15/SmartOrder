using SmartOrder.Business.Configuration.Services;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class ProductCostingIssuesPage : ContentPage
{
    private readonly CostingService _costingService;

    public ProductCostingIssuesPage(CostingService costingService)
    {
        InitializeComponent();
        _costingService = costingService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadIssuesAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadIssuesAsync();
    }

    private async Task LoadIssuesAsync()
    {
        try
        {
            IssuesList.ItemsSource = (await _costingService.GetProductIssuesAsync()).ToList();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("ProductCostingIssuesPage.LoadIssuesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Error, Title = "No se pudo consultar", Message = "Revisa la conexion con el API." });
        }
    }
}
