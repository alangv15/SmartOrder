using Microsoft.Maui.Controls;
using SmartOrder.Business.Reports.Services;
using SmartOrder.Modules.Reports.ViewModels;

namespace SmartOrder.Modules.Reports.Pages
{
    public partial class DailySalesReportPage : ContentPage
    {
        public DailySalesReportPage(DailySalesReportService dailySalesReportService)
        {
            InitializeComponent();
            BindingContext = new DailySalesReportViewModel(dailySalesReportService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is DailySalesReportViewModel viewModel)
            {
                viewModel.LoadSalesCommand.Execute(null);
            }
        }
    }
}
