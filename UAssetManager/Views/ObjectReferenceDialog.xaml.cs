using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;
using UAssetManager.Resources;

namespace UAssetManager.Views;
public partial class ObjectReferenceDialog : Window
{
    private readonly UAsset _asset;
    private readonly ObservableCollection<ObjectReferenceItem> _items = [];
    private readonly ICollectionView _source;
    private string _searchText = string.Empty;

    public int SelectedIndex { get; private set; } = 0;

    public ObjectReferenceDialog(UAsset asset, int currentIndex = 0)
    {
        InitializeComponent();
        _asset = asset;

        var collectionViewSource = new CollectionViewSource { Source = _items };
        collectionViewSource.View.Filter += CollectionViewSource_Filter;
        ObjectsListBox.ItemsSource = _source = collectionViewSource.View;

        LoadObjectReferences();

        // If there is a current index, set as default selection
        SetDefaultSelection(currentIndex);
    }

    private void LoadObjectReferences()
    {
        _items.Clear();

        // Add empty reference option
        _items.Add(new ObjectReferenceItem
        {
            PackageIndex = new FPackageIndex(0),
            Path = StringHelper.Get("ObjectReferenceDialog.EmptyReference")
        });

        // Add import objects
        for (int i = 0; i < _asset.Imports.Count; i++)
        {
            var index = new FPackageIndex(-(i + 1));
            _items.Add(new ObjectReferenceItem
            {
                PackageIndex = index,
                Path = GetFullPathFromPackageIndex(_asset, index)
            });
        }

        // Add export objects
        for (int i = 0; i < _asset.Exports.Count; i++)
        {
            var index = new FPackageIndex(i + 1);
            _items.Add(new ObjectReferenceItem
            {
                PackageIndex = index,
                Path = GetFullPathFromPackageIndex(_asset, index)
            });
        }
    }

    private bool CollectionViewSource_Filter(object item)
    {
        if (item is ObjectReferenceItem objItem)
        {
            if (string.IsNullOrEmpty(_searchText))
            {
                return true;
            }
            else if (int.TryParse(_searchText, out int index))
            {
                return objItem.PackageIndex.Index == index;
            }
            else
            {
                return objItem.Path.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
            }
        }
        return false;
    }

    private void SetDefaultSelection(int currentIndex)
    {
        // Find matching item in the collection
        var matchingItem = _items.FirstOrDefault(item => item.PackageIndex.Index == currentIndex);
        if (matchingItem != null)
        {
            ObjectsListBox.SelectedItem = matchingItem;
            ObjectsListBox.ScrollIntoView(matchingItem);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchTextBox.Text;
        _source.Refresh();
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (ObjectsListBox.SelectedItem is ObjectReferenceItem selectedItem)
        {
            SelectedIndex = selectedItem.PackageIndex.Index;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show(StringHelper.Get("ObjectReferenceDialog.SelectPrompt"), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void AddReferenceButton_Click(object sender, RoutedEventArgs e)
    {
        var inputDialog = new TextPromptWindow
        {
            Title = StringHelper.Get("ObjectReferenceDialog.AddReferenceTitle"),
            DisplayText = StringHelper.Get("ObjectReferenceDialog.AddReferencePrompt"),
            Owner = this
        };

        if (inputDialog.ShowDialog() == true)
        {
            string fullPath = inputDialog.OutputText?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(fullPath))
            {
                MessageBox.Show(StringHelper.Get("ObjectReferenceDialog.InvalidPath"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var result = ParseAndAddReference(fullPath);

                // Refresh the list display
                LoadObjectReferences();
                _source.Refresh();

                // Select the new added reference
                var newItem = _items.FirstOrDefault(item => item.PackageIndex.Index == result.Index);
                if (newItem != null)
                {
                    ObjectsListBox.SelectedItem = newItem;
                    ObjectsListBox.ScrollIntoView(newItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(StringHelper.Get("ObjectReferenceDialog.PathParseError"), ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public static string GetFullPathFromPackageIndex(UAsset asset, FPackageIndex index)
    {
        try
        {
            if (asset == null) return index.ToString();
            else if (index.IsNull()) return "None";
            else if (index.IsExport())
            {
                var exp = index.ToExport(asset);
                if (exp == null) return "None";

                string parent = GetFullPathFromPackageIndex(asset, exp.OuterIndex);
                string self = exp.ObjectName.ToString();
                return string.IsNullOrEmpty(parent) || parent == "None" ? self : parent + "." + self;
            }
            else if (index.IsImport())
            {
                var imp = index.ToImport(asset);
                if (imp == null) return "None";

                string parent = GetFullPathFromPackageIndex(asset, imp.OuterIndex);
                string self = imp.ObjectName.ToString();
                return string.IsNullOrEmpty(parent) || parent == "None" ? self : parent + "." + self;
            }
        }
        catch
        {
            // Ignore errors
        }

        return index.ToString();
    }

    private (int Index, string Type) ParseAndAddReference(string fullPath)
    {
        var parts = fullPath.Split('.');
        if (parts.Length < 2)
        {
            throw new ArgumentException(StringHelper.Get("ObjectReferenceDialog.InvalidPathFormat"));
        }

        // For now, always create Import since we can't reliably determine if it's Export
        // TODO: Implement proper package path detection for Export vs Import
        bool isExport = false;

        // Create object chain starting with package
        int currentIndex = 0;

        for (int i = 0; i < parts.Length; i++)
        {
            currentIndex = FindOrCreateObject(parts[i], currentIndex, isExport, i == 0);
        }

        return (currentIndex, isExport ? "Export" : "Import");
    }

    private int FindOrCreateObject(string objectName, int outerIndex, bool isExport, bool isPackage)
    {
        if (isExport)
        {
            // Check if Export already exists
            for (int i = 0; i < _asset.Exports.Count; i++)
            {
                var export = _asset.Exports[i];
                if (export.OuterIndex.Index == outerIndex && string.Equals(export.ObjectName.ToString(), objectName, StringComparison.OrdinalIgnoreCase)) 
                    return i + 1;
            }

            // Create new Export
            var newExport = new Export
            {
                ObjectName = FName.FromString(_asset, objectName),
                OuterIndex = new FPackageIndex(outerIndex),
                ClassIndex = new FPackageIndex(0),
                SuperIndex = new FPackageIndex(0),
                TemplateIndex = new FPackageIndex(0),
            };

            _asset.Exports.Add(newExport);
            return _asset.Exports.Count;
        }
        else
        {
            // Check if Import already exists
            for (int i = 0; i < _asset.Imports.Count; i++)
            {
                var import = _asset.Imports[i];
                if (import.OuterIndex.Index == outerIndex && string.Equals(import.ObjectName.ToString(), objectName, StringComparison.OrdinalIgnoreCase))
                    return -(i + 1);
            }

            // Create new Import
            var newImport = new Import(
                isPackage ? "/Script/CoreUObject" : "/Script/Engine",
                isPackage ? "Package" : "Object",
                new FPackageIndex(outerIndex),
                objectName,
                false,
                _asset
            );

            _asset.Imports.Add(newImport);
            return -_asset.Imports.Count;
        }
    }
}

public class ObjectReferenceItem
{
    public string Path { get; set; } = string.Empty;

    public required FPackageIndex PackageIndex { get; set; }
}