using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls.Editors;
internal class EnumPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new ComboBox
    {
        MinWidth = 160,
        IsEditable = false,
        ItemsSource = GetEnumDisplayItems(asset, (property as EnumPropertyData)?.EnumType)
    };

    public override DependencyProperty GetDependencyProperty() => ComboBox.SelectedItemProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is EnumPropertyData ep)
        {
            return ep.Value;
        }

        if (value is FName f) return f;
        if (value is string s) return FName.DefineDummy(asset, s);
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        FName? result = value as FName;
        if (result == null && value is string s) result = FName.DefineDummy(asset, s);
        if (parameter is EnumPropertyData ep)
        {
            if (result != null) ep.Value = result;
            return Binding.DoNothing;
        }
        return result ?? value;
    }

    internal static IEnumerable<FName>? GetEnumDisplayItems(UAsset asset, FName? enumType)
    {
        if (asset == null || asset.Mappings == null || enumType == null) return null;
        if (!asset.Mappings.EnumMap.TryGetValue(enumType.ToString(), out var enumMap) || enumMap?.Values == null) return null;

        return enumMap.Values.OrderBy(p => p.Key).Select(p => FName.DefineDummy(asset, p.Value));
    }
}