using System.Collections.ObjectModel;
using System.Windows;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Views;
public partial class MapStructTypeOverrideWindow
{
    private ObservableCollection<MapStructTypeOverrideItem> _items = new();

    public MapStructTypeOverrideWindow()
    {
        InitializeComponent();
        MapStructDataGrid.ItemsSource = _items;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        try
        {
            // Load existing overrides from config
            var overrides = GetMapStructTypeOverrides();
            
            _items.Clear();
            foreach (var kvp in overrides)
            {
                _items.Add(new MapStructTypeOverrideItem
                {
                    MapPropertyName = kvp.Key,
                    KeyType = kvp.Value.Item1?.Value,
                    ValueType = kvp.Value.Item2?.Value
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading overrides: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveToConfig()
    {
        try
        {
            var overrides = new Dictionary<string, Tuple<FString, FString>>();
            
            foreach (var item in _items)
            {
                if (!string.IsNullOrWhiteSpace(item.MapPropertyName))
                {
                    overrides[item.MapPropertyName] = new Tuple<FString, FString>(
                        new FString(item.KeyType),
                        new FString(item.ValueType)
                    );
                }
            }
            
            SetMapStructTypeOverrides(overrides);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving overrides: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        _items.Add(new MapStructTypeOverrideItem
        {
            MapPropertyName = "NewMapProperty",
            KeyType = "FString",
            ValueType = "FString"
        });
    }

    private void RemoveRow_Click(object sender, RoutedEventArgs e)
    {
        if (MapStructDataGrid.SelectedItem is MapStructTypeOverrideItem selectedItem)
        {
            _items.Remove(selectedItem);
        }
        else
        {
            MessageBox.Show("Please select a row to remove.", "No Selection", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to clear all overrides?", 
            "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            _items.Clear();
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        SaveToConfig();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // Static methods to manage global overrides
    private static Dictionary<string, Tuple<FString, FString>>? _mapStructTypeOverride;

    public static Dictionary<string, Tuple<FString, FString>> GetMapStructTypeOverrides()
    {
        if (_mapStructTypeOverride == null)
        {
            _mapStructTypeOverride = new UAsset().MapStructTypeOverride;
        }
        return _mapStructTypeOverride;
    }

    public static void SetMapStructTypeOverrides(Dictionary<string, Tuple<FString, FString>> overrides)
    {
        _mapStructTypeOverride = overrides;
    }
}

public class MapStructTypeOverrideItem
{
    public string MapPropertyName { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public string ValueType { get; set; } = string.Empty;
}