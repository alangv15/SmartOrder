namespace SmartOrder.Entities.Orders.Models
{
    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Pieces { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? ProductionStartDate { get; set; }
        public DateTime? ProductionEndDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string OrderStatusCode { get; set; } = string.Empty;
        public string PaymentStatusCode { get; set; } = string.Empty;
        public bool IsInternalProduction { get; set; }
        public string ProductSummary { get; set; } = string.Empty;
        public string? Comments { get; set; }
    }
}
