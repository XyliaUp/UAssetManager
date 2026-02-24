using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;

namespace UAssetManager.Controls.Editors;
internal class StructPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
	public override FrameworkElement CreateElement(PropertyData property)
	{
		switch (property)
		{
			case StructPropertyData sp when sp.StructType.ToString().Equals("RawDistributionVector", StringComparison.OrdinalIgnoreCase):
				var name = sp.Name.ToString();
				var isColor = name.Contains("color", StringComparison.OrdinalIgnoreCase);
				return new DistributionVectorEditor
				{
					IsColorMode = isColor,
					DataContext = sp
				};
			case StructPropertyData sp when sp.StructType.ToString().Equals("RawDistributionFloat", StringComparison.OrdinalIgnoreCase):
				return new DistributionFloatEditor() { DataContext = sp };

			default: return new PropertyEditor() { Asset = asset };
		}
	}

	public override DependencyProperty GetDependencyProperty() => PropertyEditor.SourceProperty;

	public override IValueConverter GetConverter() => this;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is PropertyData[] array) return array;
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value;
	}
}
