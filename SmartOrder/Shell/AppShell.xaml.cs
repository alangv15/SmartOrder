using SmartOrder.Modules.Configuration.Pages;
using SmartOrder.Modules.Orders.Pages;
using SmartOrder.Modules.Reports.Pages;
using SmartOrder.Modules.Sales.Pages;
using SmartOrder.Modules.Security.Pages;
using SmartOrder.Modules.Security.Services;
using SmartOrder.Shared.Catalogs;
using SmartOrder.Shared.Services;

namespace SmartOrder.Shell;

public partial class AppShell : Microsoft.Maui.Controls.Shell
{
    private readonly SessionService _sessionService = new();
    private IReadOnlySet<string> _permissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _isReportsExpanded;
    private bool _isConfigurationExpanded;
    private bool _isSecurityExpanded;

    public AppShell()
    {
        InitializeComponent();
        BindingContext = this;
        Routing.RegisterRoute("CategoryListPage", typeof(CategoryListPage));
        Routing.RegisterRoute("BranchListPage", typeof(BranchListPage));
        Routing.RegisterRoute("ProductListPage", typeof(ProductListPage));
        Routing.RegisterRoute("CustomerListPage", typeof(CustomerListPage));
        Routing.RegisterRoute("SalesFormPage", typeof(SaleFormPage));
        Routing.RegisterRoute("OrderListPage", typeof(OrderListPage));
        Routing.RegisterRoute("OrderFormPage", typeof(OrderFormPage));
        Routing.RegisterRoute("DailySalesReportPage", typeof(DailySalesReportPage));
        Routing.RegisterRoute("SalesSummaryReportPage", typeof(SalesSummaryReportPage));
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("UserListPage", typeof(UserListPage));
        Routing.RegisterRoute("RoleListPage", typeof(RoleListPage));
        Routing.RegisterRoute("AccessMatrixPage", typeof(AccessMatrixPage));
    }

    public bool IsReportsExpanded
    {
        get => _isReportsExpanded;
        set
        {
            if (_isReportsExpanded != value)
            {
                _isReportsExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ReportsChevron));
            }
        }
    }

    public bool IsConfigurationExpanded
    {
        get => _isConfigurationExpanded;
        set
        {
            if (_isConfigurationExpanded != value)
            {
                _isConfigurationExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConfigurationChevron));
            }
        }
    }

    public bool IsSecurityExpanded
    {
        get => _isSecurityExpanded;
        set
        {
            if (_isSecurityExpanded != value)
            {
                _isSecurityExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SecurityChevron));
            }
        }
    }

    public string ReportsChevron => IsReportsExpanded ? "v" : ">";
    public string ConfigurationChevron => IsConfigurationExpanded ? "v" : ">";
    public string SecurityChevron => IsSecurityExpanded ? "v" : ">";
    public bool CanAccessSales => HasPermission(PermissionCatalog.SalesAccess);
    public bool CanAccessOrders => HasPermission(PermissionCatalog.OrdersAccess);
    public bool CanAccessReports => HasPermission(PermissionCatalog.ReportsAccess);
    public bool CanViewDailyReport => HasPermission(PermissionCatalog.ReportsDailyView);
    public bool CanViewSalesSummaryReport => HasPermission(PermissionCatalog.ReportsSalesSummaryView);
    public bool CanAccessConfiguration => HasPermission(PermissionCatalog.ConfigurationAccess);
    public bool CanManageCategories => HasPermission(PermissionCatalog.CategoriesManage);
    public bool CanManageBranches => HasPermission(PermissionCatalog.BranchesManage);
    public bool CanManageProducts => HasPermission(PermissionCatalog.ProductsManage);
    public bool CanManageCustomers => HasPermission(PermissionCatalog.CustomersManage);
    public bool CanAccessSecurity => HasPermission(PermissionCatalog.SecurityAccess);
    public bool CanManageUsers => HasPermission(PermissionCatalog.UsersManage);
    public bool CanManageRoles => HasPermission(PermissionCatalog.RolesManage);
    public bool CanManageAccess => HasPermission(PermissionCatalog.AccessManage);

    public async Task RefreshSecurityAsync()
    {
        _permissionCodes = await _sessionService.GetPermissionCodesAsync();
        OnPropertyChanged(nameof(CanAccessSales));
        OnPropertyChanged(nameof(CanAccessOrders));
        OnPropertyChanged(nameof(CanAccessReports));
        OnPropertyChanged(nameof(CanViewDailyReport));
        OnPropertyChanged(nameof(CanViewSalesSummaryReport));
        OnPropertyChanged(nameof(CanAccessConfiguration));
        OnPropertyChanged(nameof(CanManageCategories));
        OnPropertyChanged(nameof(CanManageBranches));
        OnPropertyChanged(nameof(CanManageProducts));
        OnPropertyChanged(nameof(CanManageCustomers));
        OnPropertyChanged(nameof(CanAccessSecurity));
        OnPropertyChanged(nameof(CanManageUsers));
        OnPropertyChanged(nameof(CanManageRoles));
        OnPropertyChanged(nameof(CanManageAccess));
    }

    public async Task NavigateToDefaultAllowedRouteAsync()
    {
        await RefreshSecurityAsync();

        if (CanAccessSales)
        {
            await NavigateAsync("//SalesFormPage", "AppShell.NavigateToDefaultAllowedRouteAsync");
            return;
        }

        if (CanAccessOrders)
        {
            await NavigateAsync("//OrderListPage", "AppShell.NavigateToDefaultAllowedRouteAsync");
            return;
        }

        if (CanViewDailyReport)
        {
            await NavigateAsync("//DailySalesReportPage", "AppShell.NavigateToDefaultAllowedRouteAsync");
            return;
        }

        await AppMessageService.ShowAsync(new AppMessageOptions
        {
            Type = AppMessageType.Error,
            Title = "Sin permisos",
            Message = "Tu usuario no tiene permisos operativos asignados. Pide a un administrador que revise tu rol."
        });
    }

    protected override void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        var requiredPermission = GetRequiredPermission(args.Target.Location.OriginalString);
        if (requiredPermission is null || HasPermission(requiredPermission))
        {
            return;
        }

        args.Cancel();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Acceso denegado",
                Message = "Tu rol no tiene permiso para abrir esta pantalla."
            });
        });
    }

    private async void OnSalesTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//SalesFormPage", PermissionCatalog.SalesAccess, "AppShell.OnSalesTapped");
    }

    private async void OnOrdersTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//OrderListPage", PermissionCatalog.OrdersAccess, "AppShell.OnOrdersTapped");
    }

    private void OnReportsTapped(object sender, TappedEventArgs e)
    {
        IsReportsExpanded = !IsReportsExpanded;
    }

    private void OnConfigurationTapped(object sender, TappedEventArgs e)
    {
        IsConfigurationExpanded = !IsConfigurationExpanded;
    }

    private void OnSecurityTapped(object sender, TappedEventArgs e)
    {
        IsSecurityExpanded = !IsSecurityExpanded;
    }

    private async void OnDailyReportTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//DailySalesReportPage", PermissionCatalog.ReportsDailyView, "AppShell.OnDailyReportTapped");
    }

    private async void OnSalesSummaryReportTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//SalesSummaryReportPage", PermissionCatalog.ReportsSalesSummaryView, "AppShell.OnSalesSummaryReportTapped");
    }

    private async void OnBranchesTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//BranchListPage", PermissionCatalog.BranchesManage, "AppShell.OnBranchesTapped");
    }

    private async void OnCategoriesTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//CategoryListPage", PermissionCatalog.CategoriesManage, "AppShell.OnCategoriesTapped");
    }

    private async void OnProductsTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//ProductListPage", PermissionCatalog.ProductsManage, "AppShell.OnProductsTapped");
    }

    private async void OnCustomersTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//CustomerListPage", PermissionCatalog.CustomersManage, "AppShell.OnCustomersTapped");
    }

    private async void OnUsersTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//UserListPage", PermissionCatalog.UsersManage, "AppShell.OnUsersTapped");
    }

    private async void OnRolesTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//RoleListPage", PermissionCatalog.RolesManage, "AppShell.OnRolesTapped");
    }

    private async void OnAccessTapped(object sender, TappedEventArgs e)
    {
        await NavigateIfAllowedAsync("//AccessMatrixPage", PermissionCatalog.AccessManage, "AppShell.OnAccessTapped");
    }

    private bool HasPermission(string permissionCode)
    {
        return _permissionCodes.Contains(permissionCode);
    }

    private static string? GetRequiredPermission(string route)
    {
        if (route.Contains("SalesFormPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.SalesAccess;
        }

        if (route.Contains("OrderListPage", StringComparison.OrdinalIgnoreCase)
            || route.Contains("OrderFormPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.OrdersAccess;
        }

        if (route.Contains("DailySalesReportPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.ReportsDailyView;
        }

        if (route.Contains("SalesSummaryReportPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.ReportsSalesSummaryView;
        }

        if (route.Contains("CategoryListPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.CategoriesManage;
        }

        if (route.Contains("BranchListPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.BranchesManage;
        }

        if (route.Contains("ProductListPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.ProductsManage;
        }

        if (route.Contains("CustomerListPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.CustomersManage;
        }

        if (route.Contains("UserListPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.UsersManage;
        }

        if (route.Contains("RoleListPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.RolesManage;
        }

        if (route.Contains("AccessMatrixPage", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionCatalog.AccessManage;
        }

        return null;
    }

    private async Task NavigateIfAllowedAsync(string route, string permissionCode, string source)
    {
        await RefreshSecurityAsync();
        if (!HasPermission(permissionCode))
        {
            FlyoutIsPresented = false;
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "Acceso denegado",
                Message = "Tu rol no tiene permiso para abrir esta pantalla."
            });
            return;
        }

        await NavigateAsync(route, source);
    }

    private async Task NavigateAsync(string route, string source)
    {
        try
        {
            FlyoutIsPresented = false;
            await GoToAsync(route);
        }
        catch (Exception ex)
        {
            FileErrorLogger.Log(source, ex, $"Route: {route}");
            await AppMessageService.ShowAsync(new AppMessageOptions
            {
                Type = AppMessageType.Error,
                Title = "No se pudo abrir la pantalla",
                Message = "Ocurrio un error al navegar dentro de la aplicacion. Revisa el archivo smartorder-errors.log."
            });
        }
    }
}
