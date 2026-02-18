using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DevChat.Converters;

public class BooleanToGridLengthConverter : IValueConverter
{
	public double TrueWidth { get; set; } = 250;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is true ? new GridLength(TrueWidth) : new GridLength(0);
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}
