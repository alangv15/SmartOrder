using SmartOrder.Business.Security.Services;
using SmartOrder.Entities.Security.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Security.Pages;

public partial class RoleFormPage : ContentPage
{
    private readonly RoleManagementService _roleManagementService;
    private readonly int _roleId;
    private RoleDto _role = new();

    public RoleFormPage(RoleManagementService roleManagementService, int roleId = 0)
    {
        InitializeComponent();
        _roleManagementService = roleManagementService;
        _roleId = roleId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_roleId > 0)
        {
            await LoadRoleAsync();
        }
    }

    private async Task LoadRoleAsync()
    {
        try
        {
            var role = await _roleManagementService.GetByIdAsync(_roleId);
            if (role is null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Rol no encontrado",
                    Message = "No fue posible cargar la informacion del rol seleccionado."
                });
                await Navigation.PopAsync();
                return;
            }

            _role = role;
            PageTitleLabel.Text = "Editar rol";
            DeactivateButton.IsVisible = role.IsActive;

            NameEntry.Text = role.Name;
            CodeEntry.Text = role.Code;
            DescriptionEditor.Text = role.Description;
            IsActiveSwitch.IsToggled = role.IsActive;
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RoleFormPage.LoadRoleAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir el rol",
                Message = "Ocurrio un error al consultar los datos del rol."
            });
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameEntry.Text) || string.IsNullOrWhiteSpace(CodeEntry.Text))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Faltan datos",
                Message = "Captura nombre y codigo para guardar el rol."
            });
            return;
        }

        _role.Name = NameEntry.Text.Trim();
        _role.Code = CodeEntry.Text.Trim().ToUpperInvariant();
        _role.Description = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ? null : DescriptionEditor.Text.Trim();
        _role.IsActive = IsActiveSwitch.IsToggled;

        try
        {
            var response = _roleId == 0
                ? await _roleManagementService.CreateAsync(_role)
                : await _roleManagementService.UpdateAsync(_role);

            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Rol guardado",
                    Message = _roleId == 0
                        ? "El rol se creo correctamente."
                        : "Los cambios del rol se guardaron correctamente."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo guardar",
                Message = response?.Errors.FirstOrDefault() ?? "El API no devolvio una respuesta valida al guardar el rol."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RoleFormPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar el rol",
                Message = "Ocurrio un error inesperado al guardar el rol."
            });
        }
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_roleId <= 0)
        {
            return;
        }

        try
        {
            var response = await _roleManagementService.DeactivateAsync(_roleId);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Rol desactivado",
                    Message = "El rol dejo de estar disponible para nuevas asignaciones."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo desactivar",
                Message = response?.Errors.FirstOrDefault() ?? "No fue posible desactivar el rol."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RoleFormPage.OnDeactivateClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al desactivar",
                Message = "Ocurrio un error inesperado al desactivar el rol."
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
