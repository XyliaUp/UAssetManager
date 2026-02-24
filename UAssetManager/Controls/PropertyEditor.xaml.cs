using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetManager.Controls.Editors;
using UAssetManager.Utils;
using UAssetManager.Views;

namespace UAssetManager.Controls;
public partial class PropertyEditor
{
    #region Constructor
    public PropertyEditor()
    {
        InitializeComponent();
    }
    #endregion

    #region Fields
    private ICollectionView? _dataView;
    private PropertyData? _item;
    #endregion

    #region Properties

    public static readonly DependencyProperty AssetProperty = DependencyProperty.Register(
        nameof(Asset), typeof(UAsset), typeof(PropertyEditor), new PropertyMetadata(default(UAsset)));

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ICollection<PropertyData>), typeof(PropertyEditor), new PropertyMetadata(default, OnSourceChanged));

    public UAsset Asset
    {
        get => (UAsset?)GetValue(AssetProperty) ?? throw new ArgumentNullException(nameof(Asset));
        set => SetValue(AssetProperty, value);
    }

    public ICollection<PropertyData> Source
    {
        get => (ICollection<PropertyData>)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    #endregion

    #region Methods

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (PropertyEditor)d;
        ctl.UpdateItems(e.NewValue);
    }

    private void UpdateItems(object obj)
    {
        if (obj is ICollection<PropertyData> properties)
        {
            ItemsControl.ItemsSource = _dataView = CollectionViewSource.GetDefaultView(
                properties.Select(o => new PropertyItem() { Asset = Asset, Property = o }));
        }
    }

    private void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        _item = null;

        // get target item
        var hitTestResult = VisualTreeHelper.HitTest(ItemsControl, Mouse.GetPosition(ItemsControl));
        if (hitTestResult != null)
        {
            var item = VisualUtils.FindVisualParent<PropertyItem>(hitTestResult.VisualHit);
            if (item != null) _item = item.Property;

			// disable context menu for array items
			var array = VisualUtils.FindVisualParent<ArrayEditor>(hitTestResult.VisualHit);
			if (array != null) MenuHolder.IsOpen = false; 
		}

        // update menu
        Delete.SetVisual(_item != null);
    }

    private void AddProperty(object sender, RoutedEventArgs e)
    {
        var dialog = new AddPropertyDialog(Asset) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            Source.Add(dialog.Result!);
            _dataView?.Refresh();
        }
    }

    private void DeleteProperty(object sender, RoutedEventArgs e)
    {
        if (_item == null) return;

        Source.Remove(_item);
        _dataView?.Refresh();
    }

    #endregion
}