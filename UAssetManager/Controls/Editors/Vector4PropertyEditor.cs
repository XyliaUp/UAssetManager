using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class Vector4PropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {
        ToolTip = StringHelper.Get("Tooltip.Vector4")
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FVector4 vector4) return $"{vector4.X:F3}, {vector4.Y:F3}, {vector4.Z:F3}, {vector4.W:F3}";
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            if (parameter is Vector4PropertyData data)
            {
                try
                {
                    var parts = str.Split(',');
                    if (parts.Length == 4)
                    {
                        if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float x) &&
                            float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float y) &&
                            float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float z) &&
                            float.TryParse(parts[3].Trim(), NumberStyles.Float, culture, out float w))
                        {
                            var newVector4 = new FVector4(x, y, z, w);
                            data.Value = newVector4;
                            return newVector4;
                        }
                    }
                }
                catch
                {
                    return data.Value;
                }
            }
        }

        return value;
    }
}