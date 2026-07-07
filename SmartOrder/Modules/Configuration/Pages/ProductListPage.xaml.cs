using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class ProductListPage : ContentPage
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly List<ProductDto> _allProducts = new();
    private readonly List<CategoryDto> _categories = new();
    private readonly List<FilterOption> _directSaleOptions =
    [
        new("Sin seleccion", null),
        new("Mostrador", true),
        new("Catalogo", false)
    ];
    private readonly List<FilterOption> _activeOptions =
    [
        new("Sin seleccion", null),
        new("Activo", true),
        new("Inactivo", false)
    ];
    private int _selectedCategoryId;

    public ProductListPage(ProductService productService, CategoryService categoryService)
    {
        InitializeComponent();
        _productService = productService;
        _categoryService = categoryService;

        DirectSaleFilterPicker.ItemDisplayBinding = new Binding(nameof(FilterOption.Label));
        DirectSaleFilterPicker.ItemsSource = _directSaleOptions;
        DirectSaleFilterPicker.SelectedItem = _directSaleOptions[0];

        ActiveFilterPicker.ItemDisplayBinding = new Binding(nameof(FilterOption.Label));
        ActiveFilterPicker.ItemsSource = _activeOptions;
        ActiveFilterPicker.SelectedItem = _activeOptions[0];
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            _categories.Clear();
            _categories.AddRange((await _categoryService.GetAllAsync())
                .Where(category => category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name));

            _allProducts.Clear();
            _allProducts.AddRange(await _productService.GetAllAsync());

            EnsureSelectedCategory();
            RenderCategoryFilters();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("ProductListPage.LoadDataAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los productos",
                Message = "Ocurrio un error al consultar los productos. Revisa la conexion con el API."
            });
        }
    }

    private void EnsureSelectedCategory()
    {
        if (_categories.Count == 0)
        {
            _selectedCategoryId = 0;
            return;
        }

        if (_selectedCategoryId == 0 || !_categories.Any(category => category.CategoryId == _selectedCategoryId))
        {
            _selectedCategoryId = _categories[0].CategoryId;
        }
    }

    private void RenderCategoryFilters()
    {
        CategoryFilterContainer.Children.Clear();

        foreach (var category in _categories)
        {
            var isSelected = category.CategoryId == _selectedCategoryId;

            var button = new Button
            {
                Text = category.Name,
                Padding = new Thickness(18, 10),
                CornerRadius = 18,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                HeightRequest = 42,
                BackgroundColor = isSelected ? Color.FromArgb("#0B6E4F") : Color.FromArgb("#EAF2FB"),
                TextColor = isSelected ? Colors.White : Color.FromArgb("#255E7A"),
                BorderColor = isSelected ? Color.FromArgb("#0B6E4F") : Color.FromArgb("#BFD6EA"),
                BorderWidth = 1
            };

            button.Clicked += (_, _) =>
            {
                _selectedCategoryId = category.CategoryId;
                RenderCategoryFilters();
                ApplyFilters();
            };

            CategoryFilterContainer.Children.Add(button);
        }
    }

    private void ApplyFilters()
    {
        IEnumerable<ProductDto> filteredProducts = _allProducts;

        if (_selectedCategoryId > 0)
        {
            filteredProducts = filteredProducts.Where(product => product.CategoryId == _selectedCategoryId);
        }

        if (DirectSaleFilterPicker.SelectedItem is FilterOption directSaleOption && directSaleOption.Value.HasValue)
        {
            filteredProducts = filteredProducts.Where(product => product.IsDirectSale == directSaleOption.Value.Value);
        }

        if (ActiveFilterPicker.SelectedItem is FilterOption activeOption && activeOption.Value.HasValue)
        {
            filteredProducts = filteredProducts.Where(product => product.IsActive == activeOption.Value.Value);
        }

        ProductList.ItemsSource = filteredProducts
            .OrderBy(product => product.Name)
            .Select(product => new ProductListItemViewModel(product))
            .ToList();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProductFormPage(_productService, _categoryService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is ProductListItemViewModel selected)
        {
            await Navigation.PushAsync(new ProductFormPage(_productService, _categoryService, selected.ProductId));
            ProductList.SelectedItem = null;
        }
    }

    private void OnDirectSaleFilterChanged(object sender, EventArgs e)
    {
        ApplyFilters();
    }

    private void OnActiveFilterChanged(object sender, EventArgs e)
    {
        ApplyFilters();
    }

    private sealed record FilterOption(string Label, bool? Value);

    private sealed class ProductListItemViewModel
    {
        public ProductListItemViewModel(ProductDto product)
        {
            ProductId = product.ProductId;
            Name = product.Name;
            CategoryName = product.CategoryName;
            Sku = product.Sku;
            SalePrice = product.SalePrice;
            Description = string.IsNullOrWhiteSpace(product.Description) ? "Sin descripcion" : product.Description;
            IsActive = product.IsActive;
            IsDirectSale = product.IsDirectSale;
            UpdatedSummary = product.UpdatedAt.HasValue
                ? $"Actualizado: {product.UpdatedAt:dd/MM/yyyy HH:mm}"
                : $"Creado: {product.CreatedAt:dd/MM/yyyy}";
        }

        public int ProductId { get; }
        public string Name { get; }
        public string CategoryName { get; }
        public string Sku { get; }
        public decimal SalePrice { get; }
        public string Description { get; }
        public bool IsActive { get; }
        public bool IsDirectSale { get; }
        public string ProductSummary => $"{CategoryName} | SKU: {Sku}";
        public string DirectSaleLabel => IsDirectSale ? "Mostrador" : "Catalogo";
        public Color DirectSaleBackgroundColor => IsDirectSale ? Color.FromArgb("#0B6E4F") : Color.FromArgb("#627D98");
        public string UpdatedSummary { get; }
    }
}
