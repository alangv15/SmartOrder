using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace SmartOrder
{
    public class BoolToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Activo" : "Inactivo";
            return "Desconocido";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.Equals("Activo", StringComparison.OrdinalIgnoreCase);
            return false;
        }
    }
}