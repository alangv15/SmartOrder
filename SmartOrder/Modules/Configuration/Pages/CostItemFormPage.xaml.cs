using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;
using System.Globalization;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class CostItemFormPage : ContentPage
{
    private readonly CostingService _costingService;
    private readonly int _costItemId;
    private CostItemDto? _costItem;
    private bool _loaded;
    private bool _saving;

    public CostItemFormPage(CostingService costingService, int costItemId = 0)
    {
        InitializeComponent();
        _costingService = costingService;
        _costItemId = costItemId;
        TypePicker.ItemDisplayBinding = new Binding(nameof(CostingOption.Name));
        UnitPicker.ItemDisplayBinding = new Binding(nameof(CostingOption.Name));
        TypePicker.ItemsSource = CostingCatalog.Types.ToList();
        TypePicker.SelectedIndex = 0;
        EffectiveFromPicker.Date = DateTime.Today;
        QuantityEntry.TextChanged += (_, _) => RefreshUnitCost();
        CostEntry.TextChanged += (_, _) => RefreshUnitCost();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded) return;
        _loaded = true;
        if (_costItemId > 0)
        {
            SaveButton.IsEnabled = false;
            try
            {
                await LoadCostItemAsync();
                SaveButton.IsEnabled = _costItem != null;
            }
            catch (Exception ex)
            {
                FileErrorLogger.Log("CostItemFormPage.Load", ex);
                await ShowErrorAsync(ex.Message);
                await Navigation.PopAsync();
            }
        }
    }

    private async Task LoadCostItemAsync()
    {
        _costItem = await _costingService.GetCostItemAsync(_costItemId);
        if (_costItem == null)
        {
            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Error, Title = "Concepto no encontrado", Message = "No fue posible cargar el concepto." });
            await Navigation.PopAsync();
            return;
        }

        PageTitleLabel.Text = "Editar concepto";
        NameEntry.Text = _costItem.Name;
        DescriptionEditor.Text = _costItem.Description;
        IsActiveSwitch.IsToggled = _costItem.IsActive;
        TypePicker.SelectedItem = CostingCatalog.Types.FirstOrDefault(item => item.Code == _costItem.CostItemTypeCode);
        UnitPicker.SelectedItem = CostingCatalog.Units.FirstOrDefault(item => item.Code == _costItem.UnitCode);
        TypePicker.IsEnabled = !_costItem.IsTypeLocked;
        UnitPicker.IsEnabled = !_costItem.IsUnitLocked;

        if (_costItem.CurrentCost != null)
        {
            PresentationEntry.Text = _costItem.CurrentCost.PresentationName;
            QuantityEntry.Text = _costItem.CurrentCost.PresentationQuantity.ToString("0.####", CultureInfo.InvariantCulture);
            CostEntry.Text = _costItem.CurrentCost.PresentationCost.ToString("0.######", CultureInfo.InvariantCulture);
            EffectiveFromPicker.Date = _costItem.CurrentCost.EffectiveFrom.Date;
            CostNotesEditor.Text = _costItem.CurrentCost.Notes;
        }

        CostHistoryList.ItemsSource = _costItem.CostHistory.Select(cost => new CostHistoryItem(cost, _costItem.UnitCode)).ToList();
        RefreshUnitCost();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_saving || !SaveButton.IsEnabled) return;
        if (!TryBuildRequest(out var request, out var message))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Error, Title = "Faltan datos", Message = message });
            return;
        }

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            var response = await _costingService.SaveCostItemAsync(request);
            if (response?.Success != true || response.Data <= 0)
                throw new InvalidOperationException("No se pudo guardar el concepto.");
            await AppMessageService.ShowAsync(new AppMessageOptions { Type = AppMessageType.Success, Title = "Concepto guardado", Message = "El concepto se guardo correctamente." });
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("CostItemFormPage.Save", ex);
            await ShowErrorAsync(ex.Message);
        }
        finally { _saving = false; SaveButton.IsEnabled = true; }
    }

    private bool TryBuildRequest(out SaveCostItemRequestDto request, out string message)
    {
        request = new SaveCostItemRequestDto();
        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            message = "Captura el nombre del concepto.";
            return false;
        }

        request.CostItemId = _costItemId;
        request.Name = NameEntry.Text.Trim();
        request.Description = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ? null : DescriptionEditor.Text.Trim();
        request.IsActive = IsActiveSwitch.IsToggled;
        if (TypePicker.SelectedItem is not CostingOption type || UnitPicker.SelectedItem is not CostingOption unit ||
            !CostingCatalog.IsValidCombination(type.Code, unit.Code))
        {
            message = "Selecciona el tipo de concepto y su unidad.";
            return false;
        }
        request.CostItemTypeCode = type.Code;
        request.UnitCode = unit.Code;

        var hasAnyCost = !string.IsNullOrWhiteSpace(PresentationEntry.Text) || !string.IsNullOrWhiteSpace(QuantityEntry.Text) || !string.IsNullOrWhiteSpace(CostEntry.Text);
        if (hasAnyCost)
        {
            if (string.IsNullOrWhiteSpace(PresentationEntry.Text) ||
                !TryParseDecimal(QuantityEntry.Text, out var quantity) || quantity <= 0 || quantity != decimal.Round(quantity, 4) ||
                !TryParseDecimal(CostEntry.Text, out var cost) || cost < 0 || cost != decimal.Round(cost, 6))
            {
                message = "Completa presentacion, cantidad (hasta 4 decimales) y costo (hasta 6 decimales).";
                return false;
            }

            request.CurrentCost = new SaveCostItemCostRequestDto
            {
                PresentationName = PresentationEntry.Text.Trim(),
                PresentationQuantity = quantity,
                PresentationCost = cost,
                EffectiveFrom = EffectiveFromPicker.Date ?? DateTime.Today,
                Notes = string.IsNullOrWhiteSpace(CostNotesEditor.Text) ? null : CostNotesEditor.Text.Trim()
            };
        }

        message = string.Empty;
        return true;
    }

    private void RefreshUnitCost()
    {
        var unit = UnitPicker.SelectedItem is CostingOption selected ? selected.Name.ToLowerInvariant() : "unidad";
        QuantityLabel.Text = $"Cantidad de la presentacion ({unit})";
        if (TryParseDecimal(QuantityEntry.Text, out var quantity) && quantity > 0 && TryParseDecimal(CostEntry.Text, out var cost))
        {
            UnitCostLabel.Text = $"Costo por {unit}: ${cost / quantity:N6}";
            return;
        }

        UnitCostLabel.Text = $"Costo por {unit}: $0.000000";
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

    private sealed class CostHistoryItem
    {
        public CostHistoryItem(CostItemCostDto cost, string unitCode)
        {
            var unit = CostingCatalog.UnitName(unitCode).ToLowerInvariant();
            PresentationSummary = $"{cost.PresentationName} | {cost.PresentationQuantity:0.####} {unit}";
            DateSummary = $"{cost.EffectiveFrom:dd/MM/yyyy} - {(cost.EffectiveTo.HasValue ? cost.EffectiveTo.Value.ToString("dd/MM/yyyy") : "vigente")}";
            CostSummary = $"${cost.PresentationCost:0.######} | ${cost.UnitCost:N6}/{unit}";
        }

        public string PresentationSummary { get; }
        public string DateSummary { get; }
        public string CostSummary { get; }
    }

    private void OnTypeChanged(object sender, EventArgs e)
    {
        if (TypePicker.SelectedItem is not CostingOption type) return;
        var previous = UnitPicker.SelectedItem as CostingOption;
        var units = CostingCatalog.Units.Where(unit => CostingCatalog.IsValidCombination(type.Code, unit.Code)).ToList();
        UnitPicker.ItemsSource = units;
        UnitPicker.SelectedItem = units.FirstOrDefault(unit => unit.Code == previous?.Code) ?? units.FirstOrDefault();
    }

    private void OnUnitChanged(object sender, EventArgs e) => RefreshUnitCost();
    private static Task ShowErrorAsync(string message) => AppMessageService.ShowAsync(new AppMessageOptions
    { Type = AppMessageType.Error, Title = "No se pudo completar la operacion", Message = message });
}
