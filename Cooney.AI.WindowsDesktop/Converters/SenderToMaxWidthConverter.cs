using System.Globalization;
using System.Windows.Data;
using Cooney.AI.WindowsDesktop.Models;

namespace Cooney.AI.WindowsDesktop.Converters;

public class SenderToMaxWidthConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		// Set a reasonable max width for messages (about 70-80% of typical chat width)
		return 600.0;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
