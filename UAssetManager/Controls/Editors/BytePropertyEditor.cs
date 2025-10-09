using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Utils;

namespace UAssetManager.Controls.Editors;
internal class BytePropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property)
    {
        // check enum source
        if (property is BytePropertyData bp && bp.ByteType == BytePropertyType.FName)
        {
            var source = EnumPropertyEditor.GetEnumDisplayItems(asset, bp.EnumType);
            if (source == null) Debug.WriteLine("Cannot find enum map: {0}", bp.EnumType);
            else
            {
                return new ComboBox
                {
                    MinWidth = 160,
                    IsEditable = true,
                    ItemsSource = source
                };
            }
        }

        return new TextBox().SetupValidation(ValidationType.Byte);
    }

    public override void CreateBinding(PropertyData property, DependencyObject element)
    {
        var binding = new Binding(".")
        {
            // Bind the element to itself and use converter to bridge to BytePropertyData
            Source = element,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Converter = this,
            ConverterParameter = property,
        };

        if (element is ComboBox)
        {
            BindingOperations.SetBinding(element, ComboBox.TextProperty, binding);
        }
        else
        {
            BindingOperations.SetBinding(element, TextBox.TextProperty, binding);
        }
    }

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is BytePropertyData bp)
        {
            return bp.ByteType == BytePropertyType.FName ?
                bp.EnumValue.ToString() :
                bp.Value.ToString();
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is BytePropertyData bp)
        {
            if (bp.ByteType == BytePropertyType.FName)
            {
                var result = value as FName ?? (value is string s ? FName.DefineDummy(asset, s) : null);
                if (result != null) bp.EnumValue = result;
                return Binding.DoNothing;
            }

            bp.Value = value is string str && byte.TryParse(str.Trim(), NumberStyles.Integer, culture, out byte resultByyte) ? resultByyte : (byte)0;
        }

        return Binding.DoNothing;
    }
}