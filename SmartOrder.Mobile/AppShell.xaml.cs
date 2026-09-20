using SmartOrder.Mobile.Modules.Orders.Pages;

namespace SmartOrder.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(MobileOrderFormPage), typeof(MobileOrderFormPage));
    }

    private async void OnOrdersTapped(object sender, TappedEventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("//MobileOrdersPage");
    }
}
