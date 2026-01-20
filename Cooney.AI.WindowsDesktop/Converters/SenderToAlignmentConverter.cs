using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Cooney.AI.WindowsDesktop.Models;

namespace Cooney.AI.WindowsDesktop.Converters;

public class SenderToAlignmentConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Sender sender)
		{
			return sender == Sender.User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
		}
		return HorizontalAlignment.Stretch;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
