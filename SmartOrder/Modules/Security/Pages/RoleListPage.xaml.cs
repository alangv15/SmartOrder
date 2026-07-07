using SmartOrder.Business.Security.Services;
using SmartOrder.Entities.Security.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Security.Pages;

public partial class RoleListPage : ContentPage
{
    private readonly RoleManagementService _roleManagementService;

    public RoleListPage(RoleManagementService roleManagementService)
    {
        InitializeComponent();
        _roleManagementService = roleManagementService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        try
        {
            RoleList.ItemsSource = await _roleManagementService.GetAllAsync();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("RoleListPage.LoadRolesAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los roles",
                Message = "Ocurrio un error al consultar los roles. Revisa la conexion con el API."
            });
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RoleFormPage(_roleManagementService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RoleDto selected)
        {
            await Navigation.PushAsync(new RoleFormPage(_roleManagementService, selected.RoleId));
            RoleList.SelectedItem = null;
        }
    }
}
