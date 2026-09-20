namespace SmartOrder.Entities.Orders.Models
{
    public class OrderDto
    {
        public int OrderId { get; set; }
        public int BranchId { get; set; }
        public int UserId { get; set; }
        public int? CustomerId { get; set; }
        public int Pieces { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? CashReceivedAmount { get; set; }
        public decimal? CashChangeAmount { get; set; }
        public DateTime? ProductionStartDate { get; set; }
        public DateTime? ProductionEndDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string OrderStatusCode { get; set; } = null!;
        public string PaymentStatusCode { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string SalesChannel { get; set; } = null!;
        public string? Comments { get; set; }
        public string? CustomerGender { get; set; }
        public string? CustomerAgeRange { get; set; }
        public string? CustomerType { get; set; }
        public string? AcquisitionChannel { get; set; }
        public bool IsDirectSale { get; set; }
        public bool IsInternalProduction { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
        public ICollection<OrderDiscountDto> OrderDiscounts { get; set; } = new List<OrderDiscountDto>();
    }
}

