using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cooney.AI.WindowsDesktop.Converters;

public class BoolToGridLengthConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool isCollapsed)
		{
			return new GridLength(isCollapsed ? 50 : 200);
		}
		return new GridLength(200);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
