namespace SmartOrder.Entities.Models
{
    public class DiscountTargetDto
    {
        public string DiscountTargetCode { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
