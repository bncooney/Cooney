using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cooney.AI.WindowsDesktop.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed)
        {
            // When collapsed, hide the label (Collapsed). Otherwise, Visible.
            return isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
