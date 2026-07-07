namespace SmartOrder.Entities.Orders.Models
{
    public class OrderDiscountDto
    {
        public int OrderDiscountId { get; set; }
        public int OrderId { get; set; }
        public int DiscountRuleId { get; set; }
        public decimal AppliedAmount { get; set; }
        public DateTime AppliedDate { get; set; }
    }
}

