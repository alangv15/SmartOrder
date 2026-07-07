using SmartOrder.Business.Configuration.Services;
using SmartOrder.Entities.Configuration.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Configuration.Pages;

public partial class BranchFormPage : ContentPage
{
    private readonly BranchService _branchService;
    private readonly int _branchId;
    private BranchDto _branch = new();

    public BranchFormPage(BranchService branchService, int branchId = 0)
    {
        InitializeComponent();
        _branchService = branchService;
        _branchId = branchId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_branchId > 0)
        {
            await LoadBranchAsync();
        }
    }

    private async Task LoadBranchAsync()
    {
        try
        {
            var branch = await _branchService.GetByIdAsync(_branchId);
            if (branch is null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Sucursal no encontrada",
                    Message = "No fue posible cargar la información de la sucursal seleccionada."
                });
                await Navigation.PopAsync();
                return;
            }

            _branch = branch;
            PageTitleLabel.Text = "Editar sucursal";
            DeactivateButton.IsVisible = branch.IsActive;

            NameEntry.Text = branch.Name;
            CodeEntry.Text = branch.Code;
            AddressEntry.Text = branch.Address;
            CityEntry.Text = branch.City;
            StateEntry.Text = branch.State;
            PostalCodeEntry.Text = branch.PostalCode;
            PhoneEntry.Text = branch.Phone;
            EmailEntry.Text = branch.Email;
            IsActiveSwitch.IsToggled = branch.IsActive;
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("BranchFormPage.LoadBranchAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir la sucursal",
                Message = "Ocurrió un error al consultar los datos de la sucursal."
            });
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) ||
            string.IsNullOrWhiteSpace(CodeEntry.Text))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Faltan datos",
                Message = "Captura al menos nombre y código para guardar la sucursal."
            });
            return;
        }

        _branch.Name = NameEntry.Text.Trim();
        _branch.Code = CodeEntry.Text.Trim();
        _branch.Address = GetTrimmedValue(AddressEntry.Text);
        _branch.City = GetTrimmedValue(CityEntry.Text);
        _branch.State = GetTrimmedValue(StateEntry.Text);
        _branch.PostalCode = GetTrimmedValue(PostalCodeEntry.Text);
        _branch.Phone = GetTrimmedValue(PhoneEntry.Text);
        _branch.Email = GetTrimmedValue(EmailEntry.Text);
        _branch.IsActive = IsActiveSwitch.IsToggled;

        try
        {
            var response = _branchId == 0
                ? await _branchService.CreateAsync(_branch)
                : await _branchService.UpdateAsync(_branch);

            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Sucursal guardada",
                    Message = _branchId == 0
                        ? "La sucursal se creó correctamente."
                        : "Los cambios de la sucursal se guardaron correctamente."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo guardar",
                Message = response?.Errors.FirstOrDefault() ?? "El API no devolvió una respuesta válida al guardar la sucursal."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("BranchFormPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar la sucursal",
                Message = "Ocurrió un error inesperado al guardar la sucursal."
            });
        }
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_branchId <= 0)
        {
            return;
        }

        try
        {
            var response = await _branchService.DeactivateAsync(_branchId);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Sucursal desactivada",
                    Message = "La sucursal dejó de estar disponible para la operación."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo desactivar",
                Message = response?.Errors.FirstOrDefault() ?? "No fue posible desactivar la sucursal."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("BranchFormPage.OnDeactivateClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al desactivar",
                Message = "Ocurrió un error inesperado al desactivar la sucursal."
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private static string? GetTrimmedValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
