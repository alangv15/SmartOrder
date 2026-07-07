using SmartOrder.Business.Security.Services;
using SmartOrder.Modules.Security.Services;

namespace SmartOrder.Modules.Security.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly SessionService _sessionService;
    private bool _isLoginInProgress;

    public LoginPage(AuthService authService, SessionService sessionService)
    {
        InitializeComponent();
        _authService = authService;
        _sessionService = sessionService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EmailEntry.Focus();
    }

    private void OnEmailCompleted(object sender, EventArgs e)
    {
        PasswordEntry.Focus();
    }

    private async void OnPasswordCompleted(object sender, EventArgs e)
    {
        await HandleLoginAsync();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await HandleLoginAsync();
    }

    private async Task HandleLoginAsync()
    {
        if (_isLoginInProgress)
        {
            return;
        }

        ErrorContainer.IsVisible = false;

        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Completa usuario y contraseña.";
            ErrorContainer.IsVisible = true;
            return;
        }

        _isLoginInProgress = true;
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Validando...";

        try
        {
            var result = await _authService.LoginAsync(email, password);
            if (result?.Success == true && result.Data != null)
            {
                await _sessionService.SaveUserSessionAsync(result.Data);
                if (Microsoft.Maui.Controls.Shell.Current is SmartOrder.Shell.AppShell appShell)
                {
                    await appShell.NavigateToDefaultAllowedRouteAsync();
                }

                EmailEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                return;
            }

            ErrorLabel.Text = result?.Errors?.FirstOrDefault() ?? "Credenciales inválidas.";
            ErrorContainer.IsVisible = true;
        }
        finally
        {
            _isLoginInProgress = false;
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Ingresar";
        }
    }
}
