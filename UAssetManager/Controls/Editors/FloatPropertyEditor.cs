using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetManager.Utils;

namespace UAssetManager.Controls.Editors;
internal class FloatPropertyEditor : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox().SetupValidation(ValidationType.Float);

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    public override void CreateBinding(PropertyData property, DependencyObject element) =>
        BindingOperations.SetBinding(element, GetDependencyProperty(),
            new Binding(".")
            {
                Source = property,
                Mode = GetBindingMode(property),
                UpdateSourceTrigger = GetUpdateSourceTrigger(property),
                Converter = GetConverter(),
                ConverterParameter = property,
            });

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FloatPropertyData fp) return fp.Value.ToString("G", culture);
        if (value is DoublePropertyData dp) return dp.Value.ToString("G", culture);
        return value?.ToString() ?? "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is FloatPropertyData fp)
        {
            fp.Value = (value is string str && float.TryParse(str.Trim(), NumberStyles.Float, culture, out float result)) ? result : 0.0f;
        }
        if (parameter is DoublePropertyData dp)
        {
            dp.Value = (value is string str && double.TryParse(str.Trim(), NumberStyles.Float, culture, out double result)) ? result : 0.0d;
        }

        return Binding.DoNothing;
    }
}