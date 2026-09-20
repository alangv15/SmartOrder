using SmartOrder.Business.Reports.Services;
using SmartOrder.Entities.Reports.Models;
using SmartOrder.Modules.Reports.Services;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartOrder.Modules.Reports.ViewModels
{
    public class SalesSummaryReportViewModel : INotifyPropertyChanged
    {
        private readonly SalesSummaryReportService _salesSummaryReportService;
        private DateTime _startDate;
        private DateTime _endDate;
        private bool _isLoading;
        private decimal _totalSalesAmount;
        private decimal _cashSalesAmount;
        private decimal _cardSalesAmount;
        private int _totalItemsSold;

        public SalesSummaryReportViewModel(SalesSummaryReportService salesSummaryReportService)
        {
            _salesSummaryReportService = salesSummaryReportService;
            _startDate = DateTime.Today;
            _endDate = DateTime.Today;
            LoadSalesCommand = new Command(async () => await LoadSales());
            ExportPdfCommand = new Command(async () => await ExportPdf());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                var normalized = value.Date;
                if (_startDate != normalized)
                {
                    _startDate = normalized;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                var normalized = value.Date;
                if (_endDate != normalized)
                {
                    _endDate = normalized;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal TotalSalesAmount
        {
            get => _totalSalesAmount;
            set
            {
                if (_totalSalesAmount != value)
                {
                    _totalSalesAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal CashSalesAmount
        {
            get => _cashSalesAmount;
            set
            {
                if (_cashSalesAmount != value)
                {
                    _cashSalesAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal CardSalesAmount
        {
            get => _cardSalesAmount;
            set
            {
                if (_cardSalesAmount != value)
                {
                    _cardSalesAmount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalItemsSold
        {
            get => _totalItemsSold;
            set
            {
                if (_totalItemsSold != value)
                {
                    _totalItemsSold = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<SalesSummaryDayItem> Days { get; } = new();

        public ICommand LoadSalesCommand { get; }

        public ICommand ExportPdfCommand { get; }

        private async Task LoadSales()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            try
            {
                var report = await _salesSummaryReportService.GetSalesSummaryReportAsync(StartDate, EndDate);
                ApplyReport(report);
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SalesSummaryReportViewModel.LoadSales", ex);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "No se pudo cargar el reporte",
                        Message = $"No fue posible obtener el acumulado de ventas. Detalle: {ex.Message}"
                    });
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExportPdf()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            try
            {
                var report = await _salesSummaryReportService.GetSalesSummaryReportAsync(StartDate, EndDate);
                ApplyReport(report);

                var filePath = await SalesSummaryPdfExporter.ExportAsync(new SalesSummaryPdfRequest(
                    StartDate,
                    EndDate,
                    TotalSalesAmount,
                    CashSalesAmount,
                    CardSalesAmount,
                    TotalItemsSold,
                    Days.Select(day => new SalesSummaryPdfDay(
                        day.Date,
                        day.TotalSalesAmount,
                        day.CashSalesAmount,
                        day.CardSalesAmount,
                        day.TotalItemsSold)).ToList()));

                await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(new Microsoft.Maui.ApplicationModel.OpenFileRequest
                {
                    File = new Microsoft.Maui.Storage.ReadOnlyFile(filePath)
                });

                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "PDF generado",
                    Message = "El acumulado de ventas se genero correctamente en la carpeta Documentos\\SmartOrder\\Reports."
                });
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("SalesSummaryReportViewModel.ExportPdf", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo exportar",
                    Message = "Ocurrio un error al generar el PDF del acumulado de ventas. Revisa el archivo smartorder-errors.log."
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyReport(SalesSummaryReportDto report)
        {
            StartDate = report.StartDate;
            EndDate = report.EndDate;
            TotalSalesAmount = report.TotalSalesAmount;
            CashSalesAmount = report.CashSalesAmount;
            CardSalesAmount = report.CardSalesAmount;
            TotalItemsSold = report.TotalItemsSold;

            Days.Clear();
            foreach (var day in report.Days.OrderBy(day => day.Date))
            {
                Days.Add(new SalesSummaryDayItem
                {
                    Date = day.Date,
                    TotalSalesAmount = day.TotalSalesAmount,
                    CashSalesAmount = day.CashSalesAmount,
                    CardSalesAmount = day.CardSalesAmount,
                    TotalItemsSold = day.TotalItemsSold
                });
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SalesSummaryDayItem
    {
        public DateTime Date { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal CashSalesAmount { get; set; }
        public decimal CardSalesAmount { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
