using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class Vector2DPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {
        ToolTip = StringHelper.Get("Tooltip.Vector2D")
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FVector2D vector2D) return $"{vector2D.X:F3}, {vector2D.Y:F3}";
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            try
            {
                var parts = str.Split(',');
                if (parts.Length == 2)
                {
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float x) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float y))
                    {
                        return new FVector2D(x, y);
                    }
                }
            }
            catch
            {
                // Return current value on parsing failure
            }
        }
        return value;
    }
}