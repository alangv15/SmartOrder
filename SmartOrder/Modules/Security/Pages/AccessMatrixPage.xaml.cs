using System.Collections.ObjectModel;
using SmartOrder.Business.Security.Services;
using SmartOrder.Entities.Security.Models;
using SmartOrder.Shared.Catalogs;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Security.Pages;

public partial class AccessMatrixPage : ContentPage
{
    private readonly RoleManagementService _roleManagementService;
    private readonly SecurityAccessService _securityAccessService;
    private readonly ObservableCollection<PermissionGroupViewModel> _permissionGroups = new();
    private List<RoleDto> _roles = new();
    private RoleDto? _selectedRole;

    public AccessMatrixPage(RoleManagementService roleManagementService, SecurityAccessService securityAccessService)
    {
        InitializeComponent();
        _roleManagementService = roleManagementService;
        _securityAccessService = securityAccessService;
        BindingContext = this;
    }

    public ObservableCollection<PermissionGroupViewModel> PermissionGroups => _permissionGroups;
    public bool IsEmptyStateVisible => !_permissionGroups.Any();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        try
        {
            _roles = (await _roleManagementService.GetAllAsync())
                .Where(role => role.IsActive)
                .OrderBy(role => role.Name)
                .ToList();

            RolePicker.ItemsSource = _roles;

            if (_selectedRole == null)
            {
                RolePicker.SelectedItem = _roles.FirstOrDefault();
            }
            else
            {
                RolePicker.SelectedItem = _roles.FirstOrDefault(role => role.RoleId == _selectedRole.RoleId);
            }
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("AccessMatrixPage.LoadRolesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los roles",
                Message = "Ocurrio un error al consultar los roles disponibles."
            });
        }
    }

    private async Task LoadAccessAsync()
    {
        if (_selectedRole == null)
        {
            ClearGroups();
            return;
        }

        try
        {
            var access = await _securityAccessService.GetByRoleIdAsync(_selectedRole.RoleId);
            if (access == null)
            {
                ClearGroups();
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Error,
                    Title = "No se pudo cargar el acceso",
                    Message = "No fue posible consultar los permisos del rol seleccionado."
                });
                return;
            }

            ClearGroups();

            foreach (var moduleGroup in access.Permissions
                         .GroupBy(permission => permission.Module)
                         .OrderBy(group => group.Key))
            {
                _permissionGroups.Add(new PermissionGroupViewModel(moduleGroup.Key, moduleGroup.ToList()));
            }

            base.OnPropertyChanged(nameof(IsEmptyStateVisible));
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("AccessMatrixPage.LoadAccessAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los accesos",
                Message = "Ocurrio un error al consultar la matriz de acceso del rol."
            });
        }
    }

    private void OnRoleChanged(object? sender, EventArgs e)
    {
        _selectedRole = RolePicker.SelectedItem as RoleDto;
        _ = LoadAccessAsync();
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadAccessAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_selectedRole == null)
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Info,
                Title = "Selecciona un rol",
                Message = "Primero selecciona un rol para editar sus accesos."
            });
            return;
        }

        try
        {
            var selectedPermissionIds = _permissionGroups
                .SelectMany(group => group.Permissions)
                .Where(permission => permission.IsAssigned)
                .Select(permission => permission.PermissionId)
                .ToList();

            var response = await _securityAccessService.UpdateAsync(_selectedRole.RoleId, selectedPermissionIds);
            if (response?.Success == true)
            {
                await AppMessageService.ShowAsync(new AppMessageOptions
                {
                    Type = AppMessageType.Success,
                    Title = "Accesos actualizados",
                    Message = $"Los accesos del rol {_selectedRole.Name} se guardaron correctamente."
                });
                return;
            }

            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron guardar los accesos",
                Message = response?.Errors.FirstOrDefault() ?? "El API no devolvio una respuesta valida al actualizar los accesos."
            });
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("AccessMatrixPage.OnSaveClicked", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Error al guardar los accesos",
                Message = "Ocurrio un error inesperado al actualizar los permisos del rol."
            });
        }
    }

    private void ClearGroups()
    {
        _permissionGroups.Clear();
        base.OnPropertyChanged(nameof(IsEmptyStateVisible));
    }

    public sealed class PermissionGroupViewModel
    {
        public PermissionGroupViewModel(string moduleCode, IEnumerable<PermissionDto> permissions)
        {
            ModuleCode = moduleCode;
            ModuleLabel = SecurityCatalog.GetModuleLabel(moduleCode);
            AccentColor = SecurityCatalog.GetModuleAccentColor(moduleCode);
            Permissions = permissions.ToList();
        }

        public string ModuleCode { get; }
        public string ModuleLabel { get; }
        public Color AccentColor { get; }
        public List<PermissionDto> Permissions { get; }
        public int PermissionCount => Permissions.Count;
    }
}
