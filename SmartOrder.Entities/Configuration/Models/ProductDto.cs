namespace SmartOrder.Entities.Configuration.Models
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Sku { get; set; } = null!;
        public decimal SalePrice { get; set; }
        public int? CurrentProductPriceId { get; set; }
        public decimal? CurrentSalePrice { get; set; }
        public bool HasCurrentPrice { get; set; }
        public int? CurrentProductRecipeId { get; set; }
        public decimal? CurrentUnitCost { get; set; }
        public bool HasCurrentCost { get; set; }
        public bool IsActive { get; set; }
        public bool IsDirectSale { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

