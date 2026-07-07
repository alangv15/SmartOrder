namespace SmartOrder.Entities.Orders.Models
{
    public class OrderItemDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountPerUnit { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? LineTotal { get; set; }
    }
}

