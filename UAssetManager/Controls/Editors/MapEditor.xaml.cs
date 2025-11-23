using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls.Editors;

public partial class MapEditor : UserControl
{
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(TMap<PropertyData, PropertyData>), typeof(MapEditor), 
        new PropertyMetadata(default(TMap<PropertyData, PropertyData>), OnSourceChanged));

    public static readonly DependencyProperty AssetProperty = DependencyProperty.Register(
        nameof(Asset), typeof(UAsset), typeof(MapEditor), new PropertyMetadata(default(UAsset)));

    public TMap<PropertyData, PropertyData> Source
    {
        get => (TMap<PropertyData, PropertyData>)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public UAsset Asset
    {
        get => (UAsset)GetValue(AssetProperty);
        set => SetValue(AssetProperty, value);
    }

    private ObservableCollection<MapEditorItem> _entries = new();

    public MapEditor()
    {
        InitializeComponent();
        ItemsList.ItemsSource = _entries;
    }

    public MapEditor(UAsset asset) : this()
    {
        Asset = asset;
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MapEditor editor)
        {
            editor.UpdateEntries();
        }
    }

    private void UpdateEntries()
    {
        var previous = _entries.ToDictionary(x => x.Key, x => x.IsExpanded);
        _entries.Clear();
        
        if (Source != null)
        {
            foreach (var kvp in Source)
            {
                _entries.Add(new MapEditorItem
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Asset = Asset,
                    IsExpanded = previous.TryGetValue(kvp.Key, out var exp) && exp
                });
            }
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (Source == null || Asset == null) return;
        
        var newKey = new StrPropertyData(FName.DefineDummy(Asset, "Key")) { Value = new FString(string.Empty) };
        var newValue = new StrPropertyData(FName.DefineDummy(Asset, "Value")) { Value = new FString(string.Empty) };
        
        Source[newKey] = newValue;
        UpdateEntries();
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is MapEditorItem item)
        {
            Source?.Remove(item.Key);
            UpdateEntries();
        }
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe &&
            fe.FindName("MorePopup") is Popup popup)
        {
            popup.IsOpen = !popup.IsOpen;
        }
    }

}

public class MapEditorItem
{
    public required PropertyData Key { get; set; }
    public required PropertyData Value { get; set; }
    public required UAsset? Asset { get; set; }
    public bool IsExpanded { get; set; }

    public override string? ToString() => $"{Key?.Name} -> {Value?.Name}";
    public bool IsKeyObjectProperty => Key is ObjectPropertyData;
    public bool IsValueObjectProperty => Value is ObjectPropertyData;
}

internal class MapPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new MapEditor(asset);

    public override DependencyProperty GetDependencyProperty() => MapEditor.SourceProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TMap<PropertyData, PropertyData> map)
        {
            if (parameter is MapPropertyData originalMapProperty)
            {
                originalMapProperty.Value = map;
                return originalMapProperty;
            }

            var dummyName = FName.DefineDummy(asset, "Map");
            return new MapPropertyData(dummyName) { Value = map };
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is MapPropertyData mapPropertyData)
        {
            return mapPropertyData.Value;
        }
        return value;
    }
}
