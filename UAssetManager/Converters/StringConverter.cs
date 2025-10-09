using System.Globalization;
using System.Windows.Data;
using UAssetManager.Resources;

namespace UAssetManager.Converters;
public class StringConverter : IValueConverter, IMultiValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return parameter is string s ? StringHelper.Get(s, value) : value?.ToString();
    }

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return parameter is string s ? StringHelper.Get(s, values) : values?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}