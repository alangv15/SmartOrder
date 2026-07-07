using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;
using System.Globalization;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class ProductFormPage : ContentPage
{
    private readonly ProductService _productService;
    private readonly CategoryService _categoryService;
    private readonly int _productId;
    private readonly List<CategoryDto> _categories = new();
    private ProductDto _product = new();

    public ProductFormPage(ProductService productService, CategoryService categoryService, int productId = 0)
    {
        InitializeComponent();
        _productService = productService;
        _categoryService = categoryService;
        _productId = productId;

        CategoryPicker.ItemDisplayBinding = new Binding(nameof(CategoryDto.Name));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadCategoriesAsync();

        if (_productId > 0)
        {
            await LoadProductAsync();
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            _categories.Clear();
            _categories.AddRange((await _categoryService.GetAllAsync())
                .Where(category => category.IsActive)
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name));

            CategoryPicker.ItemsSource = _categories;
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("ProductFormPage.LoadCategoriesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar las categorias",
                Message = "Ocurrio un error al consultar las categorias activas. Revisa la conexion con el API."
            });
        }
    }

    private async Task LoadProductAsync()
    {
        try
        {
            var product = await _productService.GetByIdAsync(_productId);
            if (product is null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Producto no encontrado",
                    Message = "No fue posible cargar la informacion del producto seleccionado."
                });
                await Navigation.PopAsync();
                return;
            }

            _product = product;
            PageTitleLabel.Text = "Editar producto";
            DeactivateButton.IsVisible = product.IsActive;

            NameEntry.Text = product.Name;
            SkuEntry.Text = product.Sku;
            SalePriceEntry.Text = product.SalePrice.ToString("0.##", CultureInfo.InvariantCulture);
            DescriptionEditor.Text = product.Description;
            IsDirectSaleSwitch.IsToggled = product.IsDirectSale;
            IsActiveSwitch.IsToggled = product.IsActive;

            SelectCategory(product.CategoryId);
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("ProductFormPage.LoadProductAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir el producto",
                Message = "Ocurrio un error al consultar los datos del producto."
            });
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!TryBuildProduct(out var message))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Faltan datos",
                Message = message
            });
            return;
        }

        try
        {
            var success = false;
            var errors = Enumerable.Empty<string>();

            if (_productId == 0)
            {
                var response = await _productService.CreateAsync(_product);
                success = response?.Success == true;
                errors = response?.Errors ?? Enumerable.Empty<string>();

                if (response?.Data > 0)
                {
                    _product.ProductId = response.Data;
                }
            }
            else
            {
                var response = await _productService.UpdateAsync(_product);
                success = response?.Success == true;
                errors = response?.Errors ?? Enumerable.Empty<string>();
            }

            if (success)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Producto guardado",
                    Message = _productId == 0
                        ? $"El producto se creo correctamente con folio {_product.ProductId}."
                        : "Los cambios del producto se guardaron correctamente."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo guardar",
                Message = errors.FirstOrDefault() ?? "El API no devolvio una respuesta valida al guardar el producto."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("ProductFormPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar el producto",
                Message = "Ocurrio un error inesperado al guardar el producto."
            });
        }
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_productId <= 0)
        {
            return;
        }

        try
        {
            var response = await _productService.DeactivateAsync(_productId);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Producto desactivado",
                    Message = "El producto dejo de estar disponible para la operacion."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo desactivar",
                Message = response?.Errors.FirstOrDefault() ?? "No fue posible desactivar el producto."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("ProductFormPage.OnDeactivateClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al desactivar",
                Message = "Ocurrio un error inesperado al desactivar el producto."
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private bool TryBuildProduct(out string message)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            message = "Captura el nombre del producto.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SkuEntry.Text))
        {
            message = "Captura el SKU del producto.";
            return false;
        }

        if (CategoryPicker.SelectedItem is not CategoryDto selectedCategory)
        {
            message = "Selecciona la categoria del producto.";
            return false;
        }

        if (!TryParseSalePrice(SalePriceEntry.Text, out var salePrice) || salePrice <= 0)
        {
            message = "Captura un precio de venta mayor a cero.";
            return false;
        }

        _product.Name = NameEntry.Text.Trim();
        _product.Sku = SkuEntry.Text.Trim();
        _product.CategoryId = selectedCategory.CategoryId;
        _product.CategoryName = selectedCategory.Name;
        _product.SalePrice = salePrice;
        _product.Description = GetTrimmedValue(DescriptionEditor.Text);
        _product.IsDirectSale = IsDirectSaleSwitch.IsToggled;
        _product.IsActive = IsActiveSwitch.IsToggled;

        message = string.Empty;
        return true;
    }

    private void SelectCategory(int categoryId)
    {
        var selectedCategory = _categories.FirstOrDefault(category => category.CategoryId == categoryId);
        if (selectedCategory is not null)
        {
            CategoryPicker.SelectedItem = selectedCategory;
        }
    }

    private static bool TryParseSalePrice(string? value, out decimal salePrice)
    {
        var text = (value ?? string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out salePrice) ||
            decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out salePrice);
    }

    private static string? GetTrimmedValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
