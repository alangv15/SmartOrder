using SmartOrder.Business.Reports.Services;
using SmartOrder.Shared.Catalogs;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartOrder.Modules.Reports.ViewModels
{
    public class DailySalesReportViewModel : INotifyPropertyChanged
    {
        private readonly DailySalesReportService _dailySalesReportService;
        private DateTime _selectedDate;
        private bool _isLoading;
        private int _totalItemsSold;
        private decimal _totalSalesAmount;
        private decimal _cashSalesAmount;
        private decimal _cardSalesAmount;
        private int _cashTransactionsCount;
        private int _cardTransactionsCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DailySalesReportViewModel(DailySalesReportService dailySalesReportService)
        {
            _dailySalesReportService = dailySalesReportService;
            _selectedDate = DateTime.Today;
            LoadSalesCommand = new Command(async () => await LoadSales());
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                var normalized = value.Date;
                if (_selectedDate != normalized)
                {
                    _selectedDate = normalized;
                    OnPropertyChanged();
                    _ = LoadSales();
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

        public int CashTransactionsCount
        {
            get => _cashTransactionsCount;
            set
            {
                if (_cashTransactionsCount != value)
                {
                    _cashTransactionsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CardTransactionsCount
        {
            get => _cardTransactionsCount;
            set
            {
                if (_cardTransactionsCount != value)
                {
                    _cardTransactionsCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<DailySaleItem> SalesItems { get; } = new();

        public ICommand LoadSalesCommand { get; }

        private async Task LoadSales()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            try
            {
                var report = await _dailySalesReportService.GetDailySalesReportAsync(SelectedDate);

                SalesItems.Clear();

                foreach (var item in report.Items.OrderBy(item => item.Time))
                {
                    SalesItems.Add(new DailySaleItem
                    {
                        Folio = item.Folio,
                        Category = item.Category,
                        ProductName = item.Product,
                        Quantity = item.Quantity,
                        Total = item.Total,
                        PaymentMethodCode = item.PaymentMethod,
                        Time = item.Time.ToString("HH:mm")
                    });
                }

                TotalItemsSold = report.TotalItemsSold;
                TotalSalesAmount = report.TotalSalesAmount;
                CashTransactionsCount = report.Items.Count(item => item.PaymentMethod == OrderCatalog.CashPaymentMethodCode);
                CardTransactionsCount = report.Items.Count(item => item.PaymentMethod == OrderCatalog.CardPaymentMethodCode);
                CashSalesAmount = report.Items
                    .Where(item => item.PaymentMethod == OrderCatalog.CashPaymentMethodCode)
                    .Sum(item => item.Total);
                CardSalesAmount = report.Items
                    .Where(item => item.PaymentMethod == OrderCatalog.CardPaymentMethodCode)
                    .Sum(item => item.Total);
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("DailySalesReportViewModel.LoadSales", ex);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await AppMessageService.ShowAsync(new AppMessageOptions
                    {
                        Type = AppMessageType.Error,
                        Title = "No se pudo abrir el reporte",
                        Message = $"No fue posible cargar las ventas del dia seleccionado. Detalle: {ex.Message}"
                    });
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DailySaleItem
    {
        public int Folio { get; set; }
        public string Category { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethodCode { get; set; } = string.Empty;
        public string PaymentMethodLabel => OrderCatalog.GetPaymentMethodLabel(PaymentMethodCode);
        public string PaymentMethodColorHex => OrderCatalog.GetPaymentMethodColorHex(PaymentMethodCode);
        public string Time { get; set; } = string.Empty;
    }
}
