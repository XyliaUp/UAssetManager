using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using UAssetAPI.PropertyTypes.Structs;
using UAssetManager.Models;

namespace UAssetManager.Controls.Editors;
public partial class DistributionFloatEditor : UserControl
{
	private RawDistributionFloat Model;

	public ObservableWarpCollection<float> Items { get; } = new();

	public byte Stride
	{
		get => (byte)GetValue(StrideProperty);
		set => SetValue(StrideProperty, value);
	}

	public static readonly DependencyProperty StrideProperty =
		DependencyProperty.Register(nameof(Stride), typeof(byte),
		typeof(DistributionFloatEditor), new PropertyMetadata((byte)1));


	private PlotModel _plotModel;
	private LineSeries _series;

	public DistributionFloatEditor()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
		DataList.ItemsSource = Items;

		InitializePlot();
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		OnModelChanged(e.NewValue as StructPropertyData);
	}

	private void OnModelChanged(StructPropertyData property)
	{
		Items.Clear();

		Model = RawDistributionFloat.FromProperty(property);
		if (Model == null)
		{
			RebuildPlot();
			return;
		}

		Stride = Math.Max((byte)1, Model.EntryStride);

		var values = Model.TableValues;
		for (int i = 0; i < values.Count; i++)
		{
			Items.Add(values[i]);
		}

		RebuildPlot();
	}

	private void OnItemsUpdated(object sender, RoutedEventArgs e)
	{
		if (Model == null) return;
		if (DataContext is not StructPropertyData property) return;

		Model.EntryStride = Stride <= 0 ? (byte)1 : Stride;

		var values = new List<float>(Items.Count);
		foreach (var item in Items)
		{
			values.Add(item.Value);
		}

		Model.TableValues = values;
		Model.EntryCount = (byte)Items.Count;
		Model.ApplyToProperty(property);

		RebuildPlot();
	}

	private void OnAddKeyClick(object sender, RoutedEventArgs e)
	{
		if (Model == null) return;

		if (Stride <= 0)
		{
			Stride = 1;
		}

		Items.Add(0f);
		OnItemsUpdated(sender, e);
	}

	private void InitializePlot()
	{
		_plotModel = new PlotModel();

		_plotModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Bottom,
			Title = "X"
		});

		_plotModel.Axes.Add(new LinearAxis
		{
			Position = AxisPosition.Left,
			Title = "Y"
		});

		_series = new LineSeries
		{
			MarkerType = MarkerType.None
		};

		_plotModel.Series.Add(_series);
		Plot.Model = _plotModel;
	}

	private void RebuildPlot()
	{
		if (_series == null)
		{
			return;
		}

		_series.Points.Clear();

		if (Model == null)
		{
			_plotModel.InvalidatePlot(true);
			return;
		}

		int stride = Stride > 0 ? Stride : 1;
		if (stride < 1) stride = 1;

		int count = Model.EntryCount;
		if (count <= 0)
		{
			count = Model.TableValues.Count / stride;
		}

		for (int i = 0; i < count; i++)
		{
			int baseIndex = i * stride;
			if (baseIndex >= Model.TableValues.Count) break;

			double x;
			double y;

			if (stride >= 2 && baseIndex + 1 < Model.TableValues.Count)
			{
				x = Model.TableValues[baseIndex + 0];
				y = Model.TableValues[baseIndex + 1];
			}
			else
			{
				x = i;
				y = Model.TableValues[baseIndex];
			}

			_series.Points.Add(new DataPoint(x, y));
		}

		_plotModel.InvalidatePlot(true);
	}
}