namespace SmartOrder.Entities.Orders.Models
{
    public class OrderStatusUpdateDto
    {
        public string OrderStatusCode { get; set; } = string.Empty;
        public string PaymentStatusCode { get; set; } = string.Empty;
    }
}
