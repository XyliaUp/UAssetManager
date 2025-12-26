using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class RotatorPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {
        ToolTip = StringHelper.Get("Tooltip.Rotator")
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FRotator rotator) return $"{rotator.Pitch:F3},{rotator.Yaw:F3},{rotator.Roll:F3}";
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrEmpty(str))
        {
            try
            {
                var parts = str.Split(',');
                if (parts.Length == 3)
                {
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float pitch) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float yaw) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float roll))
                        return new FRotator(pitch, yaw, roll);
                }
            }
            catch
            {

            }
        }
        return value;
    }
}