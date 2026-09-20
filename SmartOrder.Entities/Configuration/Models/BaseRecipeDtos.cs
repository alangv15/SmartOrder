namespace SmartOrder.Entities.Configuration.Models;

public sealed class BaseRecipeDto
{
    public int BaseRecipeId { get; set; }
    public int BaseRecipeGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public decimal RecipeCost { get; set; }
    public string? Notes { get; set; }
    public List<ProductRecipeItemDto> Items { get; set; } = new();
    public string DisplayName => $"{Name} | V{VersionNumber} | {RecipeCost:C2}";
}

public sealed class SaveBaseRecipeRequestDto
{
    public int BaseRecipeGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public string? Notes { get; set; }
    public List<SaveProductRecipeItemRequestDto> Items { get; set; } = new();
}

public sealed class ProductRecipeBaseDto
{
    public int BaseRecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public decimal QuantityMultiplier { get; set; }
    public decimal UnitCost { get; set; }
    public decimal BaseRecipeCost { get; set; }
}

public sealed class SaveProductRecipeBaseRequestDto
{
    public int BaseRecipeId { get; set; }
    public decimal QuantityMultiplier { get; set; }
}
