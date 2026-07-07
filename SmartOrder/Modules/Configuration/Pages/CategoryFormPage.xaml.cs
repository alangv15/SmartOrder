using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class CategoryFormPage : ContentPage
{
    private readonly CategoryService _categoryService;
    private readonly int _categoryId;
    private CategoryDto _category = new();

    public CategoryFormPage(CategoryService categoryService, int categoryId = 0)
    {
        InitializeComponent();
        _categoryService = categoryService;
        _categoryId = categoryId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_categoryId > 0)
        {
            await LoadCategoryAsync();
        }
    }

    private async Task LoadCategoryAsync()
    {
        try
        {
            var category = await _categoryService.GetByIdAsync(_categoryId);
            if (category is null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Categoria no encontrada",
                    Message = "No fue posible cargar la informacion de la categoria seleccionada."
                });
                await Navigation.PopAsync();
                return;
            }

            _category = category;
            PageTitleLabel.Text = "Editar categoria";
            DeactivateButton.IsVisible = category.IsActive;
            NameEntry.Text = category.Name;
            DescriptionEditor.Text = category.Description;
            DisplayOrderEntry.Text = category.DisplayOrder.ToString();
            IsDirectSaleSwitch.IsToggled = category.IsDirectSale;
            IsActiveSwitch.IsToggled = category.IsActive;
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CategoryFormPage.LoadCategoryAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir la categoria",
                Message = "Ocurrio un error al consultar los datos de la categoria."
            });
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!TryBuildCategory(out var message))
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

            if (_categoryId == 0)
            {
                var response = await _categoryService.CreateAsync(_category);
                success = response?.Success == true;
                errors = response?.Errors ?? Enumerable.Empty<string>();
            }
            else
            {
                var response = await _categoryService.UpdateAsync(_category);
                success = response?.Success == true;
                errors = response?.Errors ?? Enumerable.Empty<string>();
            }

            if (success)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Categoria guardada",
                    Message = _categoryId == 0
                        ? "La categoria se creo correctamente."
                        : "Los cambios de la categoria se guardaron correctamente."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo guardar",
                Message = errors.FirstOrDefault() ?? "El API no devolvio una respuesta valida al guardar la categoria."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CategoryFormPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar la categoria",
                Message = "Ocurrio un error inesperado al guardar la categoria."
            });
        }
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_categoryId <= 0)
        {
            return;
        }

        try
        {
            var response = await _categoryService.DeleteAsync(_categoryId);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Categoria desactivada",
                    Message = "La categoria dejo de estar disponible."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo desactivar",
                Message = response?.Errors.FirstOrDefault() ?? "No fue posible desactivar la categoria."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CategoryFormPage.OnDeactivateClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al desactivar",
                Message = "Ocurrio un error inesperado al desactivar la categoria."
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private bool TryBuildCategory(out string message)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            message = "Captura el nombre de la categoria.";
            return false;
        }

        if (!int.TryParse(DisplayOrderEntry.Text?.Trim(), out var displayOrder))
        {
            message = "Captura un orden numerico valido.";
            return false;
        }

        _category.Name = NameEntry.Text.Trim();
        _category.Description = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ? null : DescriptionEditor.Text.Trim();
        _category.DisplayOrder = displayOrder;
        _category.IsDirectSale = IsDirectSaleSwitch.IsToggled;
        _category.IsActive = IsActiveSwitch.IsToggled;

        message = string.Empty;
        return true;
    }
}
