namespace SmartOrder.Entities.Reports.Models
{
    public class DailySalesReportDto
    {
        public DateTime Date { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public int TotalItemsSold { get; set; }
        public ICollection<DailySalesReportItemDto> Items { get; set; } = new List<DailySalesReportItemDto>();
    }

    public class DailySalesReportItemDto
    {
        public int Folio { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Product { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime Time { get; set; }
    }
}

