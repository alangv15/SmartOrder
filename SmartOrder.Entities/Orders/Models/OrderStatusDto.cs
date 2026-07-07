namespace SmartOrder.Entities.Orders.Models
{
    public class OrderStatusDto
    {
        public string OrderStatusCode { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
