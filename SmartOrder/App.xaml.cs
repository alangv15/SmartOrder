using SmartOrder.Shared.Services;
using SmartOrder.Shell;

namespace SmartOrder
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    FileErrorLogger.Log("AppDomain.CurrentDomain.UnhandledException", exception);
                }
                else
                {
                    FileErrorLogger.LogMessage("AppDomain.CurrentDomain.UnhandledException", args.ExceptionObject?.ToString() ?? "Excepcion no identificada.");
                }
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                FileErrorLogger.Log("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };
        }

        protected override async void OnStart()
        {
            await Microsoft.Maui.Controls.Shell.Current.GoToAsync("//LoginPage");
        }
    }
}

