using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetManager.Controls.Editors;
internal class StructPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new PropertyEditor() { Asset = asset };

    public override DependencyProperty GetDependencyProperty() => PropertyEditor.SourceProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PropertyData[] array) return array;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }
}