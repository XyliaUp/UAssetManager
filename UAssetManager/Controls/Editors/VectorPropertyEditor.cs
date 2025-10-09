using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class VectorPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property)
    {
        var textBox = new TextBox
        {
            ToolTip = StringHelper.Get("Tooltip.Vector")
        };
        
        // Add keyboard shortcut support
        textBox.KeyDown += (s, e) =>
        {
            // Tab key navigation to next control
            if (e.Key == Key.Tab)
            {
                e.Handled = false;
            }
            // Enter key to confirm input
            else if (e.Key == Key.Enter)
            {
                textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                e.Handled = true;
            }
        };
        
        return textBox;
    }

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FVector vector)
        {
            // Smart formatting: show more decimal places for small values, fewer for large values
            var maxValue = Math.Max(Math.Max(Math.Abs(vector.X), Math.Abs(vector.Y)), Math.Abs(vector.Z));
            var precision = maxValue < 0.001 ? 6 : maxValue < 0.1 ? 4 : maxValue < 10 ? 3 : 1;
            
            return $"{vector.X.ToString($"F{precision}")}, {vector.Y.ToString($"F{precision}")}, {vector.Z.ToString($"F{precision}")}";
        }
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
                    if (float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float x) &&
                        float.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out float y) &&
                        float.TryParse(parts[2].Trim(), NumberStyles.Float, culture, out float z))
                    {
                        return new FVector(x, y, z);
                    }
                }
                else if (parts.Length == 1 && float.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out float singleValue))
                {
                    // Support single value input, auto-fill to all components
                    return new FVector(singleValue, singleValue, singleValue);
                }
            }
            catch (Exception ex)
            {
                // Log parsing error but don't interrupt program execution
                Debug.WriteLine($"Vector parsing failed: {ex.Message}, input: {str}");
            }
        }
        return value;
    }
}