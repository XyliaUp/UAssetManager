using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UAssetManager.Converters;

public class NumberToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is int integer && integer > 0 ? Visibility.Visible : Visibility.Collapsed;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotSupportedException();
	}
}