using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartOrder.Business.Configuration.Services;
using SmartOrder.Business.Orders.Services;
using SmartOrder.Mobile.Modules.Orders.Pages;
using SmartOrder.Mobile.Modules.Orders.ViewModels;

namespace SmartOrder.Mobile;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        var apiUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://192.168.68.200:8080/";

        builder.Services.AddSingleton<BranchService>();
        builder.Services.AddSingleton<CategoryService>();
        builder.Services.AddSingleton<CustomerService>();
        builder.Services.AddSingleton<DiscountLimitRuleService>();
        builder.Services.AddSingleton<DiscountRuleService>();
        builder.Services.AddSingleton<OrderService>();
        builder.Services.AddSingleton<OrderStatusService>();
        builder.Services.AddSingleton<PaymentStatusService>();
        builder.Services.AddSingleton<ProductService>();

        builder.Services.AddTransient<MobileOrdersViewModel>();
        builder.Services.AddTransient<MobileOrderFormViewModel>();
        builder.Services.AddTransient<MobileOrdersPage>();
        builder.Services.AddTransient<MobileOrderFormPage>();

        builder.Services.AddHttpClient<BranchService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<CategoryService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<CustomerService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<DiscountLimitRuleService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<DiscountRuleService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<OrderService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<OrderStatusService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<PaymentStatusService>(client => ConfigureApiClient(client, apiUrl));
        builder.Services.AddHttpClient<ProductService>(client => ConfigureApiClient(client, apiUrl));

#if DEBUG
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
