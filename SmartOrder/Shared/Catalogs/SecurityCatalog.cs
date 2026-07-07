using Microsoft.Maui.Graphics;

namespace SmartOrder.Shared.Catalogs;

public static class SecurityCatalog
{
    public static string GetModuleLabel(string? code)
    {
        return code switch
        {
            "Sales" => "Ventas",
            "Orders" => "Pedidos",
            "Reports" => "Reportes",
            "Configuration" => "Configuracion",
            "Security" => "Seguridad",
            _ => code ?? string.Empty
        };
    }

    public static Color GetModuleAccentColor(string? code)
    {
        return code switch
        {
            "Sales" => Color.FromArgb("#0B6E4F"),
            "Orders" => Color.FromArgb("#0B6E4F"),
            "Reports" => Color.FromArgb("#1B587C"),
            "Configuration" => Color.FromArgb("#8A6D1D"),
            "Security" => Color.FromArgb("#6B2F54"),
            _ => Color.FromArgb("#486581")
        };
    }
}
