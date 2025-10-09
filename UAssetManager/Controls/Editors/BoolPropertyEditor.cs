using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetManager.Controls.Editors;
internal class BoolPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new CheckBox();

    public override DependencyProperty GetDependencyProperty() => CheckBox.IsCheckedProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return boolValue;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue) return boolValue;
        return false;
    }
}