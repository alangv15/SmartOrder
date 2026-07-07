namespace SmartOrder.Entities.Models
{
    public class DiscountTypeDto
    {
        public string DiscountTypeCode { get; set; } = null!;
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; }
    }
}
