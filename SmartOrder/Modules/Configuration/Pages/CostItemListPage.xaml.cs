using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class CostItemListPage : ContentPage
{
    private readonly CostingService _costingService;
    private List<CostItemDto> _items = new();
    private bool _loading;

    public CostItemListPage(CostingService costingService)
    {
        InitializeComponent();
        _costingService = costingService;
        TypeFilterPicker.ItemDisplayBinding = new Binding(nameof(CostingOption.Name));
        TypeFilterPicker.ItemsSource = new[] { new CostingOption("", "Todos") }.Concat(CostingCatalog.Types).ToList();
        TypeFilterPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCostItemsAsync();
    }

    private async Task LoadCostItemsAsync()
    {
        if (_loading) return;
        _loading = true;
        try
        {
            _items = (await _costingService.GetCostItemsAsync()).ToList();
            RefreshItems();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CostItemListPage.LoadCostItemsAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los conceptos",
                Message = "Revisa la conexion con el API."
            });
        }
        finally { _loading = false; }
    }

    private void OnTypeFilterChanged(object sender, EventArgs e) => RefreshItems();

    private void RefreshItems()
    {
        var type = (TypeFilterPicker.SelectedItem as CostingOption)?.Code;
        CostItemList.ItemsSource = _items.Where(item => string.IsNullOrEmpty(type) || item.CostItemTypeCode == type)
            .Select(item => new CostItemListItem(item)).ToList();
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CostItemFormPage(_costingService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is CostItemListItem selected)
        {
            await Navigation.PushAsync(new CostItemFormPage(_costingService, selected.CostItemId));
            CostItemList.SelectedItem = null;
        }
    }

    private sealed class CostItemListItem
    {
        public CostItemListItem(CostItemDto costItem)
        {
            CostItemId = costItem.CostItemId;
            Name = costItem.Name;
            Description = $"{CostingCatalog.TypeName(costItem.CostItemTypeCode)} | {CostingCatalog.UnitName(costItem.UnitCode)}";
            StatusText = costItem.IsActive ? "Activo" : "Inactivo";
            StatusColor = costItem.IsActive ? Color.FromArgb("#0B6E4F") : Color.FromArgb("#9FB3C8");
            CurrentCostSummary = costItem.CurrentCost == null
                ? "Sin costo vigente"
                : $"{costItem.CurrentCost.PresentationName} | {costItem.CurrentCost.PresentationQuantity:0.####} {CostingCatalog.UnitName(costItem.UnitCode)} | ${costItem.CurrentCost.PresentationCost:0.######}";
            UnitCostSummary = costItem.CurrentCost == null
                ? "Sin costo"
                : $"${costItem.CurrentCost.UnitCost:N6}/{CostingCatalog.UnitName(costItem.UnitCode)}";
        }

        public int CostItemId { get; }
        public string Name { get; }
        public string Description { get; }
        public string StatusText { get; }
        public Color StatusColor { get; }
        public string CurrentCostSummary { get; }
        public string UnitCostSummary { get; }
    }
}
