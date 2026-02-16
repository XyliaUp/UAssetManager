using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetManager.Controls.Editors;
internal class ColorPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new ColorPicker
    {
        ShowAlpha = true
    };

    public override DependencyProperty GetDependencyProperty() => ColorPicker.ColorProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is System.Drawing.Color c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color c)
        {
            return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        return value;
    }
}
