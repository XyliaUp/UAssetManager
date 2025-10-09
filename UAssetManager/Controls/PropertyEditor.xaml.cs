using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetManager.Views;

namespace UAssetManager.Controls;
public partial class PropertyEditor
{
    #region Constructor
    private ICollectionView? _dataView;

    public PropertyEditor()
    {
        InitializeComponent();
    }
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
        if (obj is not ICollection<PropertyData> properties) return;

        ItemsControl.ItemsSource = _dataView = CollectionViewSource.GetDefaultView(
            properties.Select(o => new PropertyItem() { Asset = Asset, Property = o }));
    }

    private void AddProperty_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddPropertyDialog(Asset) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            Source.Add(dialog.Result!);
        }
    }
    #endregion
}