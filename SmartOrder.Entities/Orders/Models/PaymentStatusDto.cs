namespace SmartOrder.Entities.Orders.Models
{
    public class PaymentStatusDto
    {
        public string PaymentStatusCode { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
