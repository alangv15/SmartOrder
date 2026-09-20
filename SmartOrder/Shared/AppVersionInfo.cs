using Microsoft.Maui.ApplicationModel;

namespace SmartOrder.Shared;

public static class AppVersionInfo
{
    public static string PointOfSaleDisplay => $"Punto de venta · V{AppInfo.Current.VersionString}";
}
