using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls.Editors;

public partial class ArrayEditor
{
	#region Constructor
	public ArrayEditor()
	{
		InitializeComponent();
	}

	public ArrayEditor(UAsset asset) : this()
	{
		Asset = asset;
	}
	#endregion

	#region Propertirs
	public static readonly DependencyProperty AssetProperty = DependencyProperty.Register(
		nameof(Asset), typeof(UAsset), typeof(ArrayEditor), new PropertyMetadata(default(UAsset)));

	public static readonly DependencyProperty ArrayProperty = DependencyProperty.Register(
		nameof(Array), typeof(ArrayPropertyData), typeof(ArrayEditor), new PropertyMetadata(default(ArrayPropertyData), OnArrayChanged));

	public UAsset? Asset
	{
		get => (UAsset?)GetValue(AssetProperty);
		set => SetValue(AssetProperty, value);
	}

	public ArrayPropertyData? Array
	{
		get => (ArrayPropertyData?)GetValue(ArrayProperty);
		set => SetValue(ArrayProperty, value);
	}

	private static void OnArrayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var editor = (ArrayEditor)d;
		editor.RefreshItems();
	}
	#endregion

	#region Methods
	private void MoreButton_Click(object sender, EventArgs e)
	{
		if (sender is FrameworkElement fe &&
			fe.FindName("MorePopup") is Popup popup)
		{
			popup.IsOpen = !popup.IsOpen;
		}
	}

	private void Insert_Click(object sender, RoutedEventArgs e)
	{
		if (Array?.Value == null || Asset == null) return;
		int index = GetIndexFromSender(sender);
		if (index < 0) { index = Array.Value.Length; }
		InsertArrayElementAt(index);
		ReindexArray();
		RefreshItems();
		UpdateBinding();
	}

	private void Remove_Click(object sender, RoutedEventArgs e)
	{
		if (Array?.Value == null || Asset == null) return;
		int index = GetIndexFromSender(sender);
		if (index < 0 || index >= Array.Value.Length) return;

		Array.Value = Array.Value.Where((_, i2) => i2 != index).ToArray();
		ReindexArray();
		RefreshItems();
		UpdateBinding();
	}

	private void Copy_Click(object sender, RoutedEventArgs e)
	{
		if (Array?.Value == null || Asset == null) return;
		int index = GetIndexFromSender(sender);
		if (index < 0 || index >= Array.Value.Length) return;
		var source = Array.Value[index];
		var clone = CreateDefaultElementLike(source, Asset, true);
		Array.Value = Array.Value.Take(index).Concat([clone]).Concat(Array.Value.Skip(index)).ToArray();
		ReindexArray();
		RefreshItems();
		UpdateBinding();
	}

	private void UpdateBinding()
	{
		var bindingExpression = BindingOperations.GetBindingExpression(this, ArrayProperty);
		bindingExpression?.UpdateSource();
	}

	private void RefreshItems()
	{
		var previous = ItemsList.ItemsSource?.Cast<ArrayEditorItem>().ToDictionary(x => x.PropertyData, x => x.IsExpanded) ?? [];
		ItemsList.ItemsSource = Array?.Value.Select((item, index) => new ArrayEditorItem
		{
			PropertyData = item,
			Asset = Asset,
			Index = index,
			IsExpanded = previous.TryGetValue(item, out var exp) && exp
		});
	}

	private void ReindexArray()
	{
		if (Array?.Value == null || Asset == null) return;

		for (int i = 0; i < Array.Value.Length; i++)
		{
			var elem = Array.Value[i];
			if (elem == null) continue;

			elem.ArrayIndex = i;
			elem.Name = FName.DefineDummy(Asset, i.ToString());
		}
	}

	private void InsertArrayElementAt(int index)
	{
		if (Asset == null || Array == null) return;

		var length = Array.Value.Length;
		index = Math.Clamp(index, 0, length);
		var exemplar = Array.Value.LastOrDefault();
		var newElem = CreateDefaultElementLike(exemplar, Asset, true);
		Array.Value = [.. Array.Value.Take(index), newElem, .. Array.Value.Skip(index)];
	}

	private static int GetIndexFromSender(object sender)
	{
		if (sender is FrameworkElement fe)
		{
			if (fe is System.Windows.Controls.Button btn && btn.CommandParameter is int i)
			{
				return i;
			}
			if (fe.DataContext is ArrayEditorItem wrapper)
			{
				return wrapper.Index;
			}
		}
		return -1;
	}

	private static PropertyData CreateDefaultElementLike(PropertyData? exemplar, UAsset asset, bool copy) => exemplar switch
	{
		BoolPropertyData p => new BoolPropertyData(exemplar.Name) { Value = copy && p.Value },
		IntPropertyData p => new IntPropertyData(exemplar.Name) { Value = copy ? p.Value : 0 },
		FloatPropertyData p => new FloatPropertyData(exemplar.Name) { Value = copy ? p.Value : 0f },
		DoublePropertyData p => new DoublePropertyData(exemplar.Name) { Value = copy ? p.Value : 0.0 },
		ColorPropertyData p => new ColorPropertyData(exemplar.Name) { Value = copy ? p.Value : default },
		DateTimePropertyData p => new DateTimePropertyData(exemplar.Name) { Value = copy ? p.Value : default },
		GuidPropertyData p => new GuidPropertyData(exemplar.Name) { Value = copy ? p.Value : default },
		TimespanPropertyData p => new TimespanPropertyData(exemplar.Name) { Value = copy ? p.Value : default },
		StrPropertyData p => new StrPropertyData(exemplar.Name) { Value = new FString(copy ? p.Value.Value : string.Empty) },
		NamePropertyData p => new NamePropertyData(exemplar.Name) { Value = copy ? p.Value : FName.DefineDummy(asset, "None") },
		BytePropertyData p when p.ByteType == BytePropertyType.FName => new BytePropertyData(exemplar.Name)
		{
			ByteType = BytePropertyType.FName,
			EnumType = copy ? p.EnumType : FName.DefineDummy(asset, "None"),
			EnumValue = copy ? p.EnumValue : FName.DefineDummy(asset, "None"),
			Value = copy ? p.Value : (byte)0,
		},
		BytePropertyData p => new BytePropertyData(exemplar.Name) { ByteType = BytePropertyType.Byte, Value = copy ? p.Value : (byte)0 },
		ObjectPropertyData p => new ObjectPropertyData(exemplar.Name) { Value = copy ? new FPackageIndex(p.Value.Index) : new FPackageIndex(0) },
		ArrayPropertyData p => new ArrayPropertyData(exemplar.Name) { Value = copy ? [.. p.Value.Select(child => CreateDefaultElementLike(child, asset, true))] : [] },
		StructPropertyData p => new StructPropertyData(exemplar.Name, p.StructType) { Value = [.. p.Value.Select(child => CreateDefaultElementLike(child, asset, false))] },
		_ when exemplar != null => new StrPropertyData(exemplar.Name) { Value = new FString(string.Empty) },
		_ => new StrPropertyData(FName.DefineDummy(asset, "0")) { Value = new FString(string.Empty) }
	};
	#endregion
}

public class ArrayEditorItem
{
	public required PropertyData PropertyData { get; set; }
	public required UAsset? Asset { get; set; }
	public required int Index { get; set; }
	public bool IsExpanded { get; set; }

	public override string? ToString() => PropertyData?.Name.ToString();
	public bool IsObjectProperty => PropertyData is ObjectPropertyData;
	public bool IsStructProperty => PropertyData is StructPropertyData;
}

public class ArrayPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
	public override FrameworkElement CreateElement(PropertyData property) => new ArrayEditor(asset);

	public override DependencyProperty GetDependencyProperty() => ArrayEditor.ArrayProperty;

	public override IValueConverter GetConverter() => this;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is PropertyData[] array)
		{
			if (parameter is ArrayPropertyData originalArrayProperty)
			{
				originalArrayProperty.Value = array;
				return originalArrayProperty;
			}

			var dummyName = FName.DefineDummy(asset, "Array");
			return new ArrayPropertyData(dummyName) { Value = array };
		}
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is ArrayPropertyData arrayPropertyData)
		{
			return arrayPropertyData.Value;
		}
		return value;
	}
}