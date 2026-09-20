using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Business.Reports.Services;
using SmartOrder.Business.Security.Services;
using SmartOrder.Modules.Security.Services;
using SmartOrder.Shared.Printing;

namespace SmartOrder
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var apiUrl = builder.Configuration["ApiSettings:BaseUrl"]
                ?? throw new InvalidOperationException("No se encontro la configuracion ApiSettings:BaseUrl en appsettings.json.");

            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<BranchService>();
            builder.Services.AddSingleton<CategoryService>();
            builder.Services.AddSingleton<CostingService>();
            builder.Services.AddSingleton<CustomerService>();
            builder.Services.AddSingleton<DiscountLimitRuleService>();
            builder.Services.AddSingleton<DiscountRuleService>();
            builder.Services.AddSingleton<DailySalesReportService>();
            builder.Services.AddSingleton<MonthlyProfitReportService>();
            builder.Services.AddSingleton<SalesSummaryReportService>();
            builder.Services.AddSingleton<OrderService>();
            builder.Services.AddSingleton<OrderStatusService>();
            builder.Services.AddSingleton<PaymentStatusService>();
            builder.Services.AddSingleton<ProductService>();
            builder.Services.AddSingleton<RoleManagementService>();
            builder.Services.AddSingleton<SecurityAccessService>();
            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<UserManagementService>();
            builder.Services.AddSingleton<EscPosTicketFormatter>();
            builder.Services.AddSingleton<ITicketPortWriter, WindowsComTicketPortWriter>();
            builder.Services.AddSingleton<WindowsPrinterTicketWriter>();
            builder.Services.AddSingleton<IUserPrinterSettingsService, UserPrinterSettingsService>();
            builder.Services.AddSingleton<IWindowsPrinterDiscoveryService, WindowsPrinterDiscoveryService>();
            builder.Services.AddSingleton<ITicketPrinterService, TicketPrinterService>();

            builder.Services.AddHttpClient<AuthService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<BranchService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<CategoryService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<CostingService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<CustomerService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<DiscountLimitRuleService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<DiscountRuleService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<DailySalesReportService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<MonthlyProfitReportService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<SalesSummaryReportService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<OrderService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<OrderStatusService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<PaymentStatusService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<ProductService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<RoleManagementService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<SecurityAccessService>(client => ConfigureApiClient(client, apiUrl));
            builder.Services.AddHttpClient<UserManagementService>(client => ConfigureApiClient(client, apiUrl));
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void ConfigureApiClient(HttpClient client, string apiUrl)
        {
            client.BaseAddress = new Uri(apiUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
        }
    }
}
