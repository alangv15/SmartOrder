namespace SmartOrder.Entities.Reports.Models
{
    public class SalesSummaryReportDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal CashSalesAmount { get; set; }
        public decimal CardSalesAmount { get; set; }
        public int TotalItemsSold { get; set; }
        public ICollection<SalesSummaryReportDayDto> Days { get; set; } = new List<SalesSummaryReportDayDto>();
    }

    public class SalesSummaryReportDayDto
    {
        public DateTime Date { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal CashSalesAmount { get; set; }
        public decimal CardSalesAmount { get; set; }
        public int TotalItemsSold { get; set; }
    }
}
