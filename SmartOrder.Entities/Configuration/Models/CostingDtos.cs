namespace SmartOrder.Entities.Configuration.Models;

public sealed class CostItemDto
{
    public string CostItemTypeCode { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public bool IsUnitLocked { get; set; }
    public bool IsTypeLocked { get; set; }
    public int CostItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public CostItemCostDto? CurrentCost { get; set; }
    public List<CostItemCostDto> CostHistory { get; set; } = new();
}

public sealed class CostItemCostDto
{
    public int CostItemCostId { get; set; }
    public int CostItemId { get; set; }
    public string PresentationName { get; set; } = string.Empty;
    public decimal PresentationQuantity { get; set; }
    public decimal PresentationCost { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}

public sealed class SaveCostItemRequestDto
{
    public string CostItemTypeCode { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public int CostItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public SaveCostItemCostRequestDto? CurrentCost { get; set; }
}

public sealed class SaveCostItemCostRequestDto
{
    public string PresentationName { get; set; } = string.Empty;
    public decimal PresentationQuantity { get; set; }
    public decimal PresentationCost { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductRecipeDto
{
    public List<ProductRecipeBaseDto> Bases { get; set; } = new();
    public int ProductRecipeId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal RecipeCost { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public List<ProductRecipeItemDto> Items { get; set; } = new();
}

public sealed class ProductRecipeItemDto
{
    public string CostItemTypeCode { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public int ProductRecipeItemId { get; set; }
    public int ProductRecipeId { get; set; }
    public int CostItemCostId { get; set; }
    public int CostItemId { get; set; }
    public string CostItemName { get; set; } = string.Empty;
    public string PresentationName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal RecipeItemCost { get; set; }
}

public sealed class SaveProductRecipeRequestDto
{
    public List<SaveProductRecipeBaseRequestDto> Bases { get; set; } = new();
    public int ProductId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public string? Notes { get; set; }
    public List<SaveProductRecipeItemRequestDto> Items { get; set; } = new();
}

public sealed class SaveProductRecipeItemRequestDto
{
    public int CostItemCostId { get; set; }
    public decimal Quantity { get; set; }
}

public sealed class ProductCostingIssueDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
}
