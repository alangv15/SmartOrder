using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class CategoryListPage : ContentPage
{
    private readonly CategoryService _categoryService;

    public CategoryListPage(CategoryService categoryService)
    {
        InitializeComponent();
        _categoryService = categoryService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCategoriesAsync();
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await _categoryService.GetAllAsync();
            CategoryList.ItemsSource = categories
                .OrderBy(category => category.DisplayOrder)
                .ThenBy(category => category.Name)
                .Select(category => new CategoryListItemViewModel(category))
                .ToList();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CategoryListPage.LoadCategoriesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar las categorias",
                Message = "Ocurrio un error al consultar las categorias. Revisa la conexion con el API."
            });
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CategoryFormPage(_categoryService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CategoryListItemViewModel selected)
        {
            await Navigation.PushAsync(new CategoryFormPage(_categoryService, selected.CategoryId));
            CategoryList.SelectedItem = null;
        }
    }

    private sealed class CategoryListItemViewModel
    {
        public CategoryListItemViewModel(CategoryDto category)
        {
            CategoryId = category.CategoryId;
            Name = category.Name;
            DescriptionSummary = string.IsNullOrWhiteSpace(category.Description) ? "Sin descripcion" : category.Description;
            IsActive = category.IsActive;
            IsDirectSale = category.IsDirectSale;
            DisplayOrder = category.DisplayOrder;
        }

        public int CategoryId { get; }
        public string Name { get; }
        public string DescriptionSummary { get; }
        public bool IsActive { get; }
        public bool IsDirectSale { get; }
        public int DisplayOrder { get; }
        public string DirectSaleLabel => IsDirectSale ? "Mostrador" : "Catalogo";
        public Color DirectSaleBackgroundColor => IsDirectSale ? Color.FromArgb("#0B6E4F") : Color.FromArgb("#627D98");
    }
}
