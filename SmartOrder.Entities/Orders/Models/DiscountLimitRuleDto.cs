namespace SmartOrder.Entities.Orders.Models;

public class DiscountLimitRuleDto
{
    public int DiscountLimitRuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MaxDiscountPerUnit { get; set; }
    public string AdjustmentTypeCode { get; set; } = "CapAmount";
    public decimal AdjustmentValue { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ICollection<DiscountLimitTargetDto> DiscountLimitTargets { get; set; } = new List<DiscountLimitTargetDto>();
}
