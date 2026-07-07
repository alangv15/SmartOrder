using Microsoft.Maui.Controls;
using SmartOrder.Business.Reports.Services;
using SmartOrder.Modules.Reports.ViewModels;

namespace SmartOrder.Modules.Reports.Pages
{
    public partial class SalesSummaryReportPage : ContentPage
    {
        public SalesSummaryReportPage(SalesSummaryReportService salesSummaryReportService)
        {
            InitializeComponent();
            BindingContext = new SalesSummaryReportViewModel(salesSummaryReportService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is SalesSummaryReportViewModel viewModel)
            {
                viewModel.LoadSalesCommand.Execute(null);
            }
        }
    }
}
