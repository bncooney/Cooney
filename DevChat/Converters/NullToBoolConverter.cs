using System.Globalization;
using System.Windows.Data;

namespace DevChat.Converters;

public class NullToBoolConverter : IValueConverter
{
	public static readonly NullToBoolConverter Instance = new();

	public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
		=> value is not null;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}
