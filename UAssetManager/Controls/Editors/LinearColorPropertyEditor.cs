using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class LinearColorPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {
        ToolTip = StringHelper.Get("Tooltip.LinearColor")
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FLinearColor color) return $"{color.R:F3},{color.G:F3},{color.B:F3},{color.A:F3}";
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            try
            {
                var parts = str.Split(',');
                if (parts.Length == 4)
                {
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float r) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float g) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float b) &&
                        float.TryParse(parts[3].Trim(), NumberStyles.Float, culture, out float a))
                    {
                        return new FLinearColor(r, g, b, a);
                    }
                }
            }
            catch
            {
            
            }
        }
        return value;
    }
}