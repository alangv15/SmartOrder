using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class RecipeFormPage : ContentPage
{
    private readonly CostingService _costingService;
    private readonly ProductService _productService;
    private readonly int _productId;
    private readonly bool _isBaseRecipe;
    private readonly BaseRecipeDto? _baseRecipe;
    private bool _loaded;
    private bool _saving;
    private readonly ObservableCollection<RecipeBaseViewModel> _bases = new();
    private readonly List<ProductDto> _products = new();
    private readonly List<CostItemDto> _costItems = new();
    private readonly ObservableCollection<RecipeItemViewModel> _items = new();

    public RecipeFormPage(CostingService costingService, ProductService productService, int productId = 0,
        bool isBaseRecipe = false, BaseRecipeDto? baseRecipe = null)
    {
        InitializeComponent();
        _costingService = costingService;
        _productService = productService;
        _productId = productId;
        _isBaseRecipe = isBaseRecipe;
        _baseRecipe = baseRecipe;
        ProductSection.IsVisible = !isBaseRecipe;
        BaseIdentitySection.IsVisible = isBaseRecipe;
        BaseActiveSection.IsVisible = isBaseRecipe;
        BasesSection.IsVisible = !isBaseRecipe;
        if (isBaseRecipe)
        {
            Title = "Receta base";
            HeaderLabel.Text = "Receta base";
            SubtitleLabel.Text = "Materias primas, empaque y servicios compartidos por los productos";
            GeneralSectionLabel.Text = "Datos y vigencia";
            CostItemsSectionLabel.Text = "Agregar componente de costo";
            ItemsSectionLabel.Text = "Componentes de la base";
        }
        ProductPicker.IsEnabled = productId == 0;
        CostTypeSection.IsVisible = true;
        CostTypePicker.ItemDisplayBinding = new Binding(nameof(CostingOption.Name));
        CostTypePicker.ItemsSource = CostingCatalog.Types
            .Where(item => !isBaseRecipe || CostingCatalog.IsAllowedInBaseRecipe(item.Code))
            .ToList();
        CostTypePicker.SelectedIndex = 0;
        BaseRecipePicker.ItemDisplayBinding = new Binding(nameof(BaseRecipeDto.DisplayName));
        RecipeBasesList.ItemsSource = _bases;
        EffectiveFromPicker.Date = DateTime.Today;
        EffectiveFromPicker.DateSelected += (_, _) => RefreshCostItemOptions();
        ProductPicker.ItemDisplayBinding = new Binding(nameof(ProductDto.Name));
        CostItemPicker.ItemDisplayBinding = new Binding(nameof(CostItemDisplayItem.DisplayName));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EnsureLoadedAsync();
    }

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        SaveButton.IsEnabled = false;
        try
        {
            await LoadDataAsync();
            SaveButton.IsEnabled = true;
            RetryLoadButton.IsVisible = false;
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RecipeFormPage.LoadDataAsync", ex);
            RetryLoadButton.IsVisible = true;
            await ShowErrorAsync("No se pudo cargar la receta. " + ex.Message);
        }
    }

    private async Task LoadDataAsync()
    {
        _items.Clear();
        _bases.Clear();
        if (!_isBaseRecipe)
        {
            _products.Clear();
            _products.AddRange((await _productService.GetAllAsync()).Where(product => product.IsActive || product.ProductId == _productId).OrderBy(product => product.Name));
            ProductPicker.ItemsSource = _products;
            ProductPicker.SelectedItem = _products.FirstOrDefault(product => product.ProductId == _productId);
            BaseRecipePicker.ItemsSource = (await _costingService.GetBaseRecipesAsync())
                .Where(recipe => recipe.IsActive && recipe.EffectiveTo == null).ToList();
        }

        _costItems.Clear();
        _costItems.AddRange((await _costingService.GetCostItemsAsync()).Where(item => item.IsActive && item.CostHistory.Count > 0 &&
            (!_isBaseRecipe || CostingCatalog.IsAllowedInBaseRecipe(item.CostItemTypeCode))).OrderBy(item => item.Name));
        RefreshCostItemOptions();

        if (_baseRecipe != null)
        {
            BaseNameEntry.Text = _baseRecipe.Name;
            BaseCodeEntry.Text = _baseRecipe.Code;
            BaseActiveCheckBox.IsChecked = _baseRecipe.IsActive;
            NotesEditor.Text = _baseRecipe.Notes;
            EffectiveFromPicker.Date = _baseRecipe.EffectiveFrom > DateTime.Today ? _baseRecipe.EffectiveFrom : DateTime.Today;
            foreach (var item in _baseRecipe.Items) _items.Add(new RecipeItemViewModel(item, RemoveItem));
        }

        if (_productId > 0)
        {
            var recipe = await _costingService.GetCurrentRecipeAsync(_productId);
            if (recipe != null)
            {
                NotesEditor.Text = recipe.Notes;
                EffectiveFromPicker.Date = DateTime.Today;
                _items.Clear();
                foreach (var item in recipe.Items)
                {
                    _items.Add(new RecipeItemViewModel(item, RemoveItem));
                }
                foreach (var item in recipe.Bases) _bases.Add(new RecipeBaseViewModel(item, RemoveBase));
                RefreshRecipeCost();
            }
        }
        RefreshRecipeCost();
    }

    private async void OnRetryLoadClicked(object sender, EventArgs e)
    {
        RetryLoadButton.IsEnabled = false;
        _loaded = false;
        try { await EnsureLoadedAsync(); }
        finally { RetryLoadButton.IsEnabled = true; }
    }

    private async void OnAddCostItemClicked(object sender, EventArgs e)
    {
        if (CostItemPicker.SelectedItem is not CostItemDisplayItem selected || selected.CostItem.CurrentCost == null)
        {
            await ShowErrorAsync("Selecciona un concepto con costo vigente.");
            return;
        }

        if (!TryParseDecimal(QuantityEntry.Text, out var quantity) || quantity <= 0 || quantity != decimal.Round(quantity, 4))
        {
            await ShowErrorAsync("Indica una cantidad mayor a cero, con hasta 4 decimales.");
            return;
        }

        var currentCost = selected.CostItem.CurrentCost;
        if (_items.Any(item => item.CostItemId == selected.CostItem.CostItemId))
        {
            await ShowErrorAsync("El concepto ya está agregado. Quítalo para cambiar su cantidad.");
            return;
        }

        _items.Add(new RecipeItemViewModel(selected.CostItem, quantity, RemoveItem));
        QuantityEntry.Text = string.Empty;
        RefreshRecipeCost();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_saving || !_loaded || !SaveButton.IsEnabled) return;
        var product = ProductPicker.SelectedItem as ProductDto;
        if (!_isBaseRecipe && product == null)
        {
            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Error, Title = "Falta producto", Message = "Selecciona el producto de la receta." });
            return;
        }

        if (!_items.Any() && (_isBaseRecipe || !_bases.Any()))
        {
            await ShowErrorAsync(_isBaseRecipe ? "Agrega al menos un concepto." : "Agrega al menos una base o un concepto.");
            return;
        }

        if (_isBaseRecipe && (string.IsNullOrWhiteSpace(BaseNameEntry.Text) || string.IsNullOrWhiteSpace(BaseCodeEntry.Text)))
        {
            await ShowErrorAsync("El nombre y el código de la receta base son obligatorios.");
            return;
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            var request = new SaveProductRecipeRequestDto
            {
                ProductId = product?.ProductId ?? 0,
                EffectiveFrom = EffectiveFromPicker.Date ?? DateTime.Today,
                Notes = string.IsNullOrWhiteSpace(NotesEditor.Text) ? null : NotesEditor.Text.Trim(),
                Bases = _bases.Select(item => new SaveProductRecipeBaseRequestDto
                {
                    BaseRecipeId = item.BaseRecipeId,
                    QuantityMultiplier = item.QuantityMultiplier
                }).ToList(),
                Items = _items.Select(item => new SaveProductRecipeItemRequestDto
                {
                    CostItemCostId = item.CostItemCostId,
                    Quantity = item.Quantity
                }).ToList()
            };

            var response = _isBaseRecipe
                ? await _costingService.SaveBaseRecipeAsync(new SaveBaseRecipeRequestDto
                {
                    BaseRecipeGroupId = _baseRecipe?.BaseRecipeGroupId ?? 0,
                    Name = BaseNameEntry.Text.Trim(),
                    Code = BaseCodeEntry.Text.Trim(),
                    IsActive = BaseActiveCheckBox.IsChecked,
                    EffectiveFrom = request.EffectiveFrom,
                    Notes = request.Notes,
                    Items = request.Items
                })
                : await _costingService.SaveRecipeAsync(request);
            if (response?.Success == true && response.Data > 0)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Success, Title = "Receta guardada", Message = "La receta se guardó correctamente." });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Error, Title = "No se pudo guardar", Message = response?.Errors.FirstOrDefault() ?? "El API no devolvio una respuesta valida." });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RecipeFormPage.Save", ex);
            await ShowErrorAsync(ex.Message);
        }
        finally
        {
            _saving = false;
            SaveButton.IsEnabled = true;
        }
    }

    private void RemoveItem(RecipeItemViewModel item)
    {
        _items.Remove(item);
        RefreshRecipeCost();
    }

    private void RefreshRecipeCost()
    {
        RecipeItemsList.ItemsSource = _items.GroupBy(item => item.CostItemTypeCode)
            .OrderBy(group => group.Key switch { CostingCatalog.RawMaterial => 0, CostingCatalog.Resale => 1, CostingCatalog.Packaging => 2, _ => 3 })
            .Select(group => new RecipeItemGroup(group.Key, group)).ToList();
        RecipeCostLabel.Text = $"Costo receta: ${_items.Sum(item => item.RecipeItemCost) + _bases.Sum(item => item.BaseRecipeCost):N2}";
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (_saving) return;
        await Navigation.PopAsync();
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out result) ||
            decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result);
    }

    private static Task ShowErrorAsync(string message) => AppMessageService.ShowAsync(new AppMessageOptions
    { Type = AppMessageType.Error, Title = "Revisa la receta", Message = message });

    private async void OnAddBaseClicked(object sender, EventArgs e)
    {
        if (BaseRecipePicker.SelectedItem is not BaseRecipeDto recipe ||
            !TryParseDecimal(MultiplierEntry.Text, out var multiplier) || multiplier <= 0 || multiplier != decimal.Round(multiplier, 6))
        {
            await ShowErrorAsync("Selecciona una base e indica un factor mayor a cero, con hasta 6 decimales.");
            return;
        }
        if (_bases.Any(item => item.BaseRecipeId == recipe.BaseRecipeId))
        {
            await ShowErrorAsync("La base ya está agregada. Quítala para cambiar su factor.");
            return;
        }
        _bases.Add(new RecipeBaseViewModel(new ProductRecipeBaseDto
        {
            BaseRecipeId = recipe.BaseRecipeId, Name = recipe.Name, VersionNumber = recipe.VersionNumber,
            QuantityMultiplier = multiplier, UnitCost = recipe.RecipeCost,
            BaseRecipeCost = decimal.Round(multiplier * recipe.RecipeCost, 6)
        }, RemoveBase));
        RefreshRecipeCost();
    }

    private void RemoveBase(RecipeBaseViewModel item)
    {
        _bases.Remove(item);
        RefreshRecipeCost();
    }

    private sealed class RecipeBaseViewModel
    {
        private readonly ProductRecipeBaseDto _item;
        public RecipeBaseViewModel(ProductRecipeBaseDto item, Action<RecipeBaseViewModel> remove)
        {
            _item = item;
            RemoveCommand = new Command(() => remove(this));
        }
        public int BaseRecipeId => _item.BaseRecipeId;
        public string Name => _item.Name;
        public decimal QuantityMultiplier => _item.QuantityMultiplier;
        public decimal BaseRecipeCost => _item.BaseRecipeCost;
        public string DetailSummary => $"Versión {_item.VersionNumber} | Factor {QuantityMultiplier:0.######} | Base completa: {_item.UnitCost:C2}";
        public string CostSummary => $"{BaseRecipeCost:C2}";
        public ICommand RemoveCommand { get; }
    }

    private sealed class CostItemDisplayItem
    {
        public CostItemDisplayItem(CostItemDto costItem)
        {
            CostItem = costItem;
            DisplayName = $"{costItem.Name} | ${costItem.CurrentCost?.UnitCost:N6}/{CostingCatalog.UnitName(costItem.UnitCode)}";
        }

        public CostItemDto CostItem { get; }
        public string DisplayName { get; }
    }

    private sealed class RecipeItemViewModel
    {
        public RecipeItemViewModel(CostItemDto costItem, decimal quantity, Action<RecipeItemViewModel> remove)
        {
            var cost = costItem.CurrentCost!;
            CostItemCostId = cost.CostItemCostId;
            CostItemId = costItem.CostItemId;
            CostItemTypeCode = costItem.CostItemTypeCode;
            UnitCode = costItem.UnitCode;
            CostItemName = costItem.Name;
            PresentationName = cost.PresentationName;
            Quantity = quantity;
            UnitCost = cost.UnitCost;
            RecipeItemCost = decimal.Round(quantity * cost.UnitCost, 6);
            RemoveCommand = new Command(() => remove(this));
        }

        public RecipeItemViewModel(ProductRecipeItemDto item, Action<RecipeItemViewModel> remove)
        {
            CostItemCostId = item.CostItemCostId;
            CostItemId = item.CostItemId;
            CostItemTypeCode = item.CostItemTypeCode;
            UnitCode = item.UnitCode;
            CostItemName = item.CostItemName;
            PresentationName = item.PresentationName;
            Quantity = item.Quantity;
            UnitCost = item.UnitCost;
            RecipeItemCost = item.RecipeItemCost;
            RemoveCommand = new Command(() => remove(this));
        }

        public int CostItemCostId { get; }
        public int CostItemId { get; }
        public string CostItemTypeCode { get; }
        public string UnitCode { get; }
        public string CostItemName { get; }
        public string PresentationName { get; }
        public decimal Quantity { get; }
        public decimal UnitCost { get; }
        public decimal RecipeItemCost { get; }
        public string DetailSummary => $"{PresentationName} | {Quantity:0.####} {CostingCatalog.UnitName(UnitCode)} | ${UnitCost:N6}/{CostingCatalog.UnitName(UnitCode)}";
        public string CostSummary => $"${RecipeItemCost:N2}";
        public ICommand RemoveCommand { get; }
    }

    private sealed class RecipeItemGroup : List<RecipeItemViewModel>
    {
        public RecipeItemGroup(string code, IEnumerable<RecipeItemViewModel> items) : base(items)
        {
            Name = code switch
            {
                CostingCatalog.RawMaterial => "Ingredientes",
                CostingCatalog.Packaging => "Empaque",
                CostingCatalog.Service => "Servicios",
                _ => "Productos de reventa"
            };
        }
        public string Name { get; }
    }

    private void OnCostTypeChanged(object sender, EventArgs e) => RefreshCostItemOptions();

    private void RefreshCostItemOptions()
    {
        var type = (CostTypePicker.SelectedItem as CostingOption)?.Code;
        var date = (EffectiveFromPicker.Date ?? DateTime.Today).Date;
        foreach (var item in _costItems)
            item.CurrentCost = item.CostHistory.FirstOrDefault(cost => cost.EffectiveFrom <= date &&
                (cost.EffectiveTo == null || cost.EffectiveTo >= date));
        CostItemPicker.ItemsSource = _costItems.Where(item => item.CostItemTypeCode == type && item.CurrentCost != null)
            .Select(item => new CostItemDisplayItem(item)).ToList();
    }

    private void OnCostItemChanged(object sender, EventArgs e)
    {
        QuantityLabel.Text = CostItemPicker.SelectedItem is CostItemDisplayItem selected
            ? $"Cantidad ({CostingCatalog.UnitName(selected.CostItem.UnitCode)})" : "Cantidad";
    }
}
