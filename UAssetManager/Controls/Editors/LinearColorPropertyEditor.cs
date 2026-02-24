using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls.Editors;
internal class LinearColorPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new ColorPicker
    {
        ShowAlpha = true
    };

    public override DependencyProperty GetDependencyProperty() => ColorPicker.ColorProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FLinearColor c)
        {
            return Color.FromArgb(ToByte(c.A), ToByte(c.R), ToByte(c.G), ToByte(c.B));
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Color c)
        {
            return new FLinearColor(FromByte(c.R), FromByte(c.G), FromByte(c.B), FromByte(c.A));
        }

        return value;
    }

    private static byte ToByte(float v)
    {
        if (v < 0) v = 0;
        if (v > 1) v = 1;
        return (byte)Math.Round(v * 255.0f);
    }

    private static float FromByte(byte v) => v / 255.0f;
}
