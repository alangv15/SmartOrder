using SmartOrder.Business.Configuration.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public sealed class BaseRecipeListPage : RecipeListPage
{
    public BaseRecipeListPage(CostingService costingService, ProductService productService)
        : base(costingService, productService, true) { }
}
