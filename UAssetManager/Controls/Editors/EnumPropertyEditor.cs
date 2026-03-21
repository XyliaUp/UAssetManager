using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls.Editors;
internal class EnumPropertyEditor(UAsset asset) : PropertyEditorBase
{
	private TextBox? TypeBox;
	private ComboBox? ValueBox;

	public override FrameworkElement CreateElement(PropertyData property)
	{
		var panel = new StackPanel() { Orientation = Orientation.Horizontal };
		panel.Children.Add(TypeBox = new TextBox()
		{
			MinWidth = 150,
			Margin = new Thickness(0, 0, 3, 0),
			Padding = new Thickness(2),
			VerticalAlignment = VerticalAlignment.Center,
		});
		panel.Children.Add(ValueBox = new ComboBox()
		{
			IsEditable = true
		});
		return panel;
	}

	public override void CreateBinding(PropertyData property, DependencyObject element)
	{
		var type = new Binding("RawValue")
		{
			Source = property,
			Mode = GetBindingMode(property),
			UpdateSourceTrigger = GetUpdateSourceTrigger(property),
			Converter = new EnumPropertyTypeConverter(asset),
			ConverterParameter = property,
		};
		var value = new Binding("RawValue")
		{
			Source = property,
			Mode = GetBindingMode(property),
			UpdateSourceTrigger = GetUpdateSourceTrigger(property),
			Converter = new EnumPropertyValueConverter(asset),
			ConverterParameter = property,
		};

		// Add binding error handling
		type.ValidationRules.Add(new BindingValidationRule());
		value.ValidationRules.Add(new BindingValidationRule());
		BindingOperations.SetBinding(TypeBox, TextBox.TextProperty, type);
		BindingOperations.SetBinding(ValueBox, ComboBox.TextProperty, value);
	}

	private class EnumPropertyTypeConverter(UAsset asset) : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => (parameter switch
		{
			EnumPropertyData p => p.EnumType,
			BytePropertyData p when p.ByteType == BytePropertyType.FName => p.EnumType,
			FName f => f,
			_ => value,
		}).ToString();

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = new FName(asset, value?.ToString());
			if (parameter is EnumPropertyData ep) ep.EnumType = result;
			if (parameter is BytePropertyData bp) bp.EnumType = result;

			return Binding.DoNothing;
		}
	}

	private class EnumPropertyValueConverter(UAsset asset) : IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture) => (parameter switch
		{
			EnumPropertyData p => p.Value,
			BytePropertyData p when p.ByteType == BytePropertyType.FName => p.EnumValue,
			FName f => f,
			_ => value,
		}).ToString();

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var result = new FName(asset, value?.ToString());
			if (parameter is EnumPropertyData ep) ep.Value = result;
			if (parameter is BytePropertyData bp) bp.EnumValue = result;

			return Binding.DoNothing;
		}
	}
}