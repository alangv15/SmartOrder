namespace SmartOrder.Entities.Orders.Models;

public class DiscountLimitTargetDto
{
    public int DiscountLimitTargetId { get; set; }
    public int DiscountLimitRuleId { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
}
