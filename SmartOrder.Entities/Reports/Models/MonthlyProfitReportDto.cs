namespace SmartOrder.Entities.Reports.Models
{
    public class MonthlyProfitReportDto
    {
        public DateTime StartMonth { get; set; }
        public DateTime EndMonth { get; set; }
        public decimal TotalRevenueAmount { get; set; }
        public decimal CashRevenueAmount { get; set; }
        public decimal CardRevenueAmount { get; set; }
        public decimal TotalCostAmount { get; set; }
        public decimal GrossProfitAmount { get; set; }
        public decimal GrossMarginPercentage { get; set; }
        public ICollection<MonthlyProfitReportMonthDto> Months { get; set; } = new List<MonthlyProfitReportMonthDto>();
    }

    public class MonthlyProfitReportMonthDto
    {
        public DateTime Month { get; set; }
        public decimal TotalRevenueAmount { get; set; }
        public decimal CashRevenueAmount { get; set; }
        public decimal CardRevenueAmount { get; set; }
        public decimal TotalCostAmount { get; set; }
        public decimal GrossProfitAmount { get; set; }
        public decimal GrossMarginPercentage { get; set; }
    }
}
