using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class QuatPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {
        ToolTip = StringHelper.Get("Tooltip.Quat")
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FQuat quat) return $"{quat.X:F3},{quat.Y:F3},{quat.Z:F3},{quat.W:F3}";
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
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float x) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float y) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float z) &&
                        float.TryParse(parts[3].Trim(), NumberStyles.Float, culture, out float w))
                    {
                        return new FQuat(x, y, z, w);
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