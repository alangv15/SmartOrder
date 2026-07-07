using SmartOrder.Business.Security.Services;
using SmartOrder.Entities.Security.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Security.Pages;

public partial class UserFormPage : ContentPage
{
    private readonly UserManagementService _userManagementService;
    private readonly RoleManagementService _roleManagementService;
    private readonly int _userId;
    private UserDto _user = new();
    private List<RoleDto> _roles = new();

    public UserFormPage(UserManagementService userManagementService, RoleManagementService roleManagementService, int userId = 0)
    {
        InitializeComponent();
        _userManagementService = userManagementService;
        _roleManagementService = roleManagementService;
        _userId = userId;
        RolePicker.ItemDisplayBinding = new Binding(nameof(RoleDto.Name));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRolesAsync();

        if (_userId > 0)
        {
            await LoadUserAsync();
        }
        else
        {
            RolePicker.SelectedItem = _roles.FirstOrDefault(role => role.IsActive);
        }
    }

    private async Task LoadRolesAsync()
    {
        _roles = (await _roleManagementService.GetAllAsync())
            .Where(role => role.IsActive)
            .OrderBy(role => role.Name)
            .ToList();

        RolePicker.ItemsSource = _roles;
    }

    private async Task LoadUserAsync()
    {
        try
        {
            var user = await _userManagementService.GetByIdAsync(_userId);
            if (user is null)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "Usuario no encontrado",
                    Message = "No fue posible cargar la información del usuario seleccionado."
                });
                await Navigation.PopAsync();
                return;
            }

            _user = user;
            PageTitleLabel.Text = "Editar usuario";
            PasswordLabel.Text = "Nueva contraseña";
            PasswordEntry.Placeholder = "Déjala vacía para conservar la actual";
            DeactivateButton.IsVisible = user.IsActive;

            FullNameEntry.Text = user.FullName;
            EmailEntry.Text = user.Email;
            IsActiveSwitch.IsToggled = user.IsActive;

            RolePicker.SelectedItem = _roles.FirstOrDefault(role => role.RoleId == user.RoleId);
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("UserFormPage.LoadUserAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir el usuario",
                Message = "Ocurrió un error al consultar los datos del usuario."
            });
            await Navigation.PopAsync();
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var selectedRole = RolePicker.SelectedItem as RoleDto;
        if (string.IsNullOrWhiteSpace(FullNameEntry.Text) ||
            string.IsNullOrWhiteSpace(EmailEntry.Text) ||
            selectedRole == null)
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Faltan datos",
                Message = "Captura nombre, correo y rol para guardar el usuario."
            });
            return;
        }

        if (_userId == 0 && string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Falta la contraseña",
                Message = "Para crear un usuario nuevo es obligatorio capturar una contraseña."
            });
            return;
        }

        _user.FullName = FullNameEntry.Text.Trim();
        _user.Email = EmailEntry.Text.Trim();
        _user.RoleId = selectedRole.RoleId;
        _user.RoleCode = selectedRole.Code;
        _user.RoleName = selectedRole.Name;
        _user.IsActive = IsActiveSwitch.IsToggled;
        _user.Password = string.IsNullOrWhiteSpace(PasswordEntry.Text) ? null : PasswordEntry.Text;

        try
        {
            var response = _userId == 0
                ? await _userManagementService.CreateAsync(_user)
                : await _userManagementService.UpdateAsync(_user);

            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Usuario guardado",
                    Message = _userId == 0
                        ? "El usuario se creó correctamente."
                        : "Los cambios del usuario se guardaron correctamente."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo guardar",
                Message = response?.Errors.FirstOrDefault() ?? "El API no devolvió una respuesta válida al guardar el usuario."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("UserFormPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar el usuario",
                Message = "Ocurrió un error inesperado al guardar el usuario."
            });
        }
    }

    private async void OnDeactivateClicked(object sender, EventArgs e)
    {
        if (_userId <= 0)
        {
            return;
        }

        try
        {
            var response = await _userManagementService.DeactivateAsync(_userId);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Usuario desactivado",
                    Message = "El usuario dejó de estar disponible para iniciar sesión."
                });
                await Navigation.PopAsync();
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo desactivar",
                Message = response?.Errors.FirstOrDefault() ?? "No fue posible desactivar el usuario."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("UserFormPage.OnDeactivateClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al desactivar",
                Message = "Ocurrió un error inesperado al desactivar el usuario."
            });
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
