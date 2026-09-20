using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class RecipeListPage : ContentPage
{
    private readonly CostingService _costingService;
    private readonly ProductService _productService;
    private readonly bool _isBaseRecipe;
    private bool _loading;
    private bool _navigating;

    public RecipeListPage(CostingService costingService, ProductService productService)
        : this(costingService, productService, false) { }

    protected RecipeListPage(CostingService costingService, ProductService productService, bool isBaseRecipe)
    {
        InitializeComponent();
        _costingService = costingService;
        _productService = productService;
        _isBaseRecipe = isBaseRecipe;
        if (isBaseRecipe)
        {
            Title = "Recetas base";
            HeaderLabel.Text = "Recetas base";
            SubtitleLabel.Text = "Bases reutilizables, conceptos y costos";
            AddButton.Text = "Nueva receta base";
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRecipesAsync();
    }

    private async Task LoadRecipesAsync()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            RecipeList.ItemsSource = _isBaseRecipe
                ? (await _costingService.GetBaseRecipesAsync()).Where(recipe => recipe.EffectiveTo == null)
                    .Select(recipe => new RecipeListItem(recipe)).ToList()
                : (await _costingService.GetRecipesAsync())
                .Where(recipe => recipe.EffectiveTo == null)
                .Select(recipe => new RecipeListItem(recipe))
                .ToList();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RecipeListPage.LoadRecipesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Error, Title = "No se pudieron cargar las recetas", Message = "Revisa la conexion con el API." });
        }
        finally { _loading = false; }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        if (_navigating) return;
        _navigating = true;
        try { await Navigation.PushAsync(new RecipeFormPage(_costingService, _productService, isBaseRecipe: _isBaseRecipe)); }
        finally { _navigating = false; }
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (!_navigating && e.CurrentSelection.FirstOrDefault() is RecipeListItem selected)
        {
            _navigating = true;
            RecipeList.SelectedItem = null;
            try { await Navigation.PushAsync(new RecipeFormPage(_costingService, _productService, selected.ProductId,
                _isBaseRecipe, selected.BaseRecipe)); }
            finally { _navigating = false; }
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e) => await LoadRecipesAsync();

    private sealed class RecipeListItem
    {
        public RecipeListItem(ProductRecipeDto recipe)
        {
            ProductId = recipe.ProductId;
            ProductName = recipe.ProductName;
            Summary = $"Versión {recipe.VersionNumber} | Desde {recipe.EffectiveFrom:dd/MM/yyyy} | {recipe.Bases.Count} bases | {recipe.Items.Count} conceptos adicionales";
            RecipeCostSummary = $"${recipe.RecipeCost:N2}";
        }

        public RecipeListItem(BaseRecipeDto recipe)
        {
            BaseRecipe = recipe;
            ProductName = recipe.Name;
            Summary = $"{recipe.Code} | Versión {recipe.VersionNumber} | Desde {recipe.EffectiveFrom:dd/MM/yyyy} | {recipe.Items.Count} conceptos | {(recipe.IsActive ? "Activa" : "Inactiva")}";
            RecipeCostSummary = $"${recipe.RecipeCost:N2}";
        }

        public BaseRecipeDto? BaseRecipe { get; }

        public int ProductId { get; }
        public string ProductName { get; }
        public string Summary { get; }
        public string RecipeCostSummary { get; }
    }
}
