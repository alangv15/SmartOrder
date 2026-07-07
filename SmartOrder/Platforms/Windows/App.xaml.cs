using Microsoft.UI.Xaml;
using SmartOrder.Shared.Services;

namespace SmartOrder.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            this.InitializeComponent();
            UnhandledException += (_, e) =>
            {
                if (e.Exception is Exception exception)
                {
                    FileErrorLogger.Log("WinUI.UnhandledException", exception, $"Handled={e.Handled}");
                }
                else
                {
                    FileErrorLogger.LogMessage("WinUI.UnhandledException", "Se recibio una excepcion no controlada sin detalle.");
                }
            };
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}
