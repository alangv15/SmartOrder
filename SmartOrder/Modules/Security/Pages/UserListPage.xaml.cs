using SmartOrder.Business.Security.Services;
using SmartOrder.Entities.Security.Models;
using SmartOrder.Shared.Services;

namespace SmartOrder.Modules.Security.Pages;

public partial class UserListPage : ContentPage
{
    private readonly UserManagementService _userManagementService;
    private readonly RoleManagementService _roleManagementService;

    public UserListPage(UserManagementService userManagementService, RoleManagementService roleManagementService)
    {
        InitializeComponent();
        _userManagementService = userManagementService;
        _roleManagementService = roleManagementService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userManagementService.GetAllAsync();
            UserList.ItemsSource = users
                .Select(user => new UserListItemViewModel(user))
                .ToList();
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log("UserListPage.LoadUsersAsync", ex);
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudieron cargar los usuarios",
                Message = "Ocurrió un error al consultar los usuarios. Revisa la conexión con el API."
            });
        }
    }

    private async void OnAddClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new UserFormPage(_userManagementService, _roleManagementService));
    }

    private async void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is UserListItemViewModel selected)
        {
            await Navigation.PushAsync(new UserFormPage(_userManagementService, _roleManagementService, selected.UserId));
            UserList.SelectedItem = null;
        }
    }

    private sealed class UserListItemViewModel
    {
        public UserListItemViewModel(UserDto user)
        {
            UserId = user.UserId;
            FullName = user.FullName;
            Email = user.Email;
            RoleLabel = string.IsNullOrWhiteSpace(user.RoleName) ? user.RoleCode : user.RoleName;
            IsActive = user.IsActive;
            CreatedAt = user.CreatedAt;
            LastLogin = user.LastLogin;
        }

        public int UserId { get; }
        public string FullName { get; }
        public string Email { get; }
        public string RoleLabel { get; }
        public bool IsActive { get; }
        public DateTime CreatedAt { get; }
        public DateTime? LastLogin { get; }
    }
}
