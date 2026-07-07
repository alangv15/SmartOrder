using SmartOrder.Entities.Models;

namespace SmartOrder.Entities.Orders.Models
{
    public class DiscountRuleDto
    {
        public int DiscountRuleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountTypeCode { get; set; } = string.Empty;
        public string DiscountTargetCode { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public string? ConditionValue { get; set; }
        public int? MinQuantity { get; set; }
        public decimal? MinTotalAmount { get; set; }
        public bool IsAutomatic { get; set; }
        public bool IsActive { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public ICollection<DiscountRuleTargetDto> DiscountRuleTargets { get; set; } = new List<DiscountRuleTargetDto>();
    }
}
