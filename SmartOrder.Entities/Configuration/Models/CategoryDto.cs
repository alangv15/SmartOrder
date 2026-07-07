using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartOrder.Entities.Configuration.Models
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsDirectSale { get; set; }
        public int DisplayOrder { get; set; }
    }
}

