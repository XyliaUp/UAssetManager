using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class ColorPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {
        ToolTip = StringHelper.Get("Tooltip.Color")
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color color) return $"{color.R},{color.G},{color.B},{color.A}";
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
                    if (byte.TryParse(parts[0].Trim(), out byte r) &&
                        byte.TryParse(parts[1].Trim(), out byte g) &&
                        byte.TryParse(parts[2].Trim(), out byte b) &&
                        byte.TryParse(parts[3].Trim(), out byte a))
                    {
                        return Color.FromArgb(r, g, b, a);
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