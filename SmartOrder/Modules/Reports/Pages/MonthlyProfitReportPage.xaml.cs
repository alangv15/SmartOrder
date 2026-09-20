using Microsoft.Maui.Controls;
using SmartOrder.Business.Reports.Services;
using SmartOrder.Modules.Reports.ViewModels;

namespace SmartOrder.Modules.Reports.Pages
{
    public partial class MonthlyProfitReportPage : ContentPage
    {
        public MonthlyProfitReportPage(MonthlyProfitReportService reportService)
        {
            InitializeComponent();
            BindingContext = new MonthlyProfitReportViewModel(reportService);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (BindingContext is MonthlyProfitReportViewModel viewModel)
            {
                viewModel.LoadReportCommand.Execute(null);
            }
        }
    }
}
