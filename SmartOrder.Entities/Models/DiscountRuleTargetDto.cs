using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartOrder.Entities.Models
{
    public class DiscountRuleTargetDto
    {
        public int DiscountRuleTargetId { get; set; }
        public int DiscountRuleId { get; set; }
        public string TargetType { get; set; } = null!;
        public int TargetId { get; set; }
    }
}
