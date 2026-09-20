using SmartOrder.Business.Reports.Services;
using SmartOrder.Entities.Reports.Models;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace SmartOrder.Modules.Reports.ViewModels
{
    public class MonthlyProfitReportViewModel : INotifyPropertyChanged
    {
        private static readonly CultureInfo SpanishMexico = new("es-MX");
        private readonly MonthlyProfitReportService _reportService;
        private bool _isLoading;
        private string _periodLabel = "Ultimos seis meses";
        private decimal _totalRevenueAmount;
        private decimal _cashRevenueAmount;
        private decimal _cardRevenueAmount;
        private decimal _totalCostAmount;
        private decimal _grossProfitAmount;
        private decimal _grossMarginPercentage;
        private string _month1Label = string.Empty;
        private string _month2Label = string.Empty;
        private string _month3Label = string.Empty;
        private string _month4Label = string.Empty;
        private string _month5Label = string.Empty;
        private string _month6Label = string.Empty;

        public MonthlyProfitReportViewModel(MonthlyProfitReportService reportService)
        {
            _reportService = reportService;
            LoadReportCommand = new Command(async () => await LoadReport());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public string PeriodLabel
        {
            get => _periodLabel;
            set => SetField(ref _periodLabel, value);
        }

        public decimal TotalRevenueAmount
        {
            get => _totalRevenueAmount;
            set => SetField(ref _totalRevenueAmount, value);
        }

        public decimal CashRevenueAmount
        {
            get => _cashRevenueAmount;
            set => SetField(ref _cashRevenueAmount, value);
        }

        public decimal CardRevenueAmount
        {
            get => _cardRevenueAmount;
            set => SetField(ref _cardRevenueAmount, value);
        }

        public decimal TotalCostAmount
        {
            get => _totalCostAmount;
            set => SetField(ref _totalCostAmount, value);
        }

        public decimal GrossProfitAmount
        {
            get => _grossProfitAmount;
            set => SetField(ref _grossProfitAmount, value);
        }

        public decimal GrossMarginPercentage
        {
            get => _grossMarginPercentage;
            set => SetField(ref _grossMarginPercentage, value);
        }

        public string Month1Label { get => _month1Label; set => SetField(ref _month1Label, value); }
        public string Month2Label { get => _month2Label; set => SetField(ref _month2Label, value); }
        public string Month3Label { get => _month3Label; set => SetField(ref _month3Label, value); }
        public string Month4Label { get => _month4Label; set => SetField(ref _month4Label, value); }
        public string Month5Label { get => _month5Label; set => SetField(ref _month5Label, value); }
        public string Month6Label { get => _month6Label; set => SetField(ref _month6Label, value); }

        public ObservableCollection<MonthlyProfitMetricRow> Metrics { get; } = new();

        public ICommand LoadReportCommand { get; }

        private async Task LoadReport()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            try
            {
                var report = await _reportService.GetMonthlyProfitReportAsync();
                ApplyReport(report);
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("MonthlyProfitReportViewModel.LoadReport", ex);
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo cargar el reporte",
                    Message = $"No fue posible obtener la rentabilidad mensual. Detalle: {ex.Message}"
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyReport(MonthlyProfitReportDto report)
        {
            PeriodLabel = $"{FormatMonth(report.StartMonth)} a {FormatMonth(report.EndMonth)}";
            TotalRevenueAmount = report.TotalRevenueAmount;
            CashRevenueAmount = report.CashRevenueAmount;
            CardRevenueAmount = report.CardRevenueAmount;
            TotalCostAmount = report.TotalCostAmount;
            GrossProfitAmount = report.GrossProfitAmount;
            GrossMarginPercentage = report.GrossMarginPercentage;

            var months = report.Months
                .OrderBy(item => item.Month)
                .Take(6)
                .ToList();

            while (months.Count < 6)
            {
                months.Add(new MonthlyProfitReportMonthDto());
            }

            Month1Label = FormatMonthHeader(months[0].Month);
            Month2Label = FormatMonthHeader(months[1].Month);
            Month3Label = FormatMonthHeader(months[2].Month);
            Month4Label = FormatMonthHeader(months[3].Month);
            Month5Label = FormatMonthHeader(months[4].Month);
            Month6Label = FormatMonthHeader(months[5].Month);

            Metrics.Clear();
            Metrics.Add(CreateMetricRow("Ingresos", "#6C4C1D", months, month => FormatMoney(month.TotalRevenueAmount)));
            Metrics.Add(CreateMetricRow("Efectivo", "#0B6E4F", months, month => FormatMoney(month.CashRevenueAmount)));
            Metrics.Add(CreateMetricRow("Tarjeta", "#1B587C", months, month => FormatMoney(month.CardRevenueAmount)));
            Metrics.Add(CreateMetricRow("Costos", "#7A4E24", months, month => FormatMoney(month.TotalCostAmount)));
            Metrics.Add(CreateMetricRow("Utilidad bruta", "#164A68", months, month => FormatMoney(month.GrossProfitAmount)));
            Metrics.Add(CreateMetricRow("Margen bruto", "#5B3A8C", months, month => $"{month.GrossMarginPercentage:F2}%"));
        }

        private static MonthlyProfitMetricRow CreateMetricRow(
            string name,
            string colorHex,
            IReadOnlyList<MonthlyProfitReportMonthDto> months,
            Func<MonthlyProfitReportMonthDto, string> formatValue)
        {
            return new MonthlyProfitMetricRow
            {
                MetricName = name,
                ValueColorHex = colorHex,
                Month1Value = formatValue(months[0]),
                Month2Value = formatValue(months[1]),
                Month3Value = formatValue(months[2]),
                Month4Value = formatValue(months[3]),
                Month5Value = formatValue(months[4]),
                Month6Value = formatValue(months[5])
            };
        }

        private static string FormatMoney(decimal value)
        {
            return value.ToString("$#,##0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatMonthHeader(DateTime month)
        {
            return month == default ? string.Empty : FormatMonth(month);
        }

        private static string FormatMonth(DateTime month)
        {
            if (month == default)
            {
                return "Sin periodo";
            }

            var label = month.ToString("MMMM yyyy", SpanishMexico);
            return char.ToUpper(label[0], SpanishMexico) + label[1..];
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MonthlyProfitMetricRow
    {
        public string MetricName { get; set; } = string.Empty;
        public string ValueColorHex { get; set; } = "#102A43";
        public string Month1Value { get; set; } = string.Empty;
        public string Month2Value { get; set; } = string.Empty;
        public string Month3Value { get; set; } = string.Empty;
        public string Month4Value { get; set; } = string.Empty;
        public string Month5Value { get; set; } = string.Empty;
        public string Month6Value { get; set; } = string.Empty;
    }
}
