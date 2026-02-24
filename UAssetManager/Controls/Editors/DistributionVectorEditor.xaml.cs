using System.Windows;
using System.Windows.Controls;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetManager.Models;

namespace UAssetManager.Controls.Editors;
public partial class DistributionVectorEditor : UserControl
{
	private RawDistributionVector Model;

	public ObservableWarpCollection<FVector> Items { get; } = new();

	public byte Stride
	{
		get => (byte)GetValue(StrideProperty);
		set => SetValue(StrideProperty, value);
	}

	public static readonly DependencyProperty StrideProperty =
		DependencyProperty.Register(nameof(Stride), typeof(byte),
		typeof(DistributionVectorEditor), new PropertyMetadata((byte)3));

	public bool IsColorMode
	{
		get => (bool)GetValue(IsColorModeProperty);
		set => SetValue(IsColorModeProperty, value);
	}

	public static readonly DependencyProperty IsColorModeProperty =
		DependencyProperty.Register(
			nameof(IsColorMode),
			typeof(bool),
			typeof(DistributionVectorEditor),
			new PropertyMetadata(true));

	public DistributionVectorEditor()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
		DataList.ItemsSource = Items;
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		OnModelChanged(e.NewValue as StructPropertyData);
	}

	private void OnModelChanged(StructPropertyData property)
	{
		Items.Clear();

		Model = RawDistributionVector.FromProperty(property);
		if (Model == null) return;

		var stride = Model.EntryStride > 0 ? Model.EntryStride : (byte)3;
		if (stride < 3) stride = 3;

		var values = Model.TableValues;
		if (values.Count < stride) return;

		int count = Model.EntryCount;
		if (count <= 0 || count * stride > values.Count)
		{
			count = values.Count / stride;
		}

		for (int i = 0; i < count; i++)
		{
			int baseIndex = i * stride;
			if (baseIndex + 2 >= values.Count) break;

			float x = values[baseIndex + 0];
			float y = values[baseIndex + 1];
			float z = values[baseIndex + 2];

			Items.Add(new FVector(x, y, z));
		}
	}

	private void OnItemsUpdated(object sender, EventArgs e)
	{
		if (Model == null) return;
		if (DataContext is not StructPropertyData property) return;

		var stride = Model.EntryStride = Stride;

		var values = new List<float>(Items.Count * stride);
		foreach (var v in Items)
		{
			values.Add(v.Value.XFloat);
			values.Add(v.Value.YFloat);
			values.Add(v.Value.ZFloat);

			for (int i = 3; i < stride; i++)
				values.Add(0f);
		}

		Model.TableValues = values;
		Model.EntryCount = (byte)values.Count;
		Model.ApplyToProperty(property);
	}

	private void OnAddKeyClick(object sender, RoutedEventArgs e)
	{
		if (Model == null) return;

		FVector v;

		if (IsColorMode)
		{
			var rand = new Random();
			v = new FVector((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
		}
		else
		{
			v = new FVector(0f, 0f, 0f);
		}

		Items.Add(v);
		OnItemsUpdated(sender, e);
	}
}