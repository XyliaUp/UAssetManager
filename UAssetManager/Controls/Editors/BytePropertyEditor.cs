using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetManager.Utils;

namespace UAssetManager.Controls.Editors;
internal class BytePropertyEditor : PropertyEditorBase, IValueConverter
{
	public override FrameworkElement CreateElement(PropertyData property) => new TextBox().SetupValidation(ValidationType.Byte);

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
		if (parameter is BytePropertyData bp && bp.ByteType == BytePropertyType.Byte)
		{
			bp.Value = value is string str && byte.TryParse(str.Trim(), NumberStyles.Integer, culture, out byte resultByyte) ? resultByyte : (byte)0;
		}

		return Binding.DoNothing;
	}
}