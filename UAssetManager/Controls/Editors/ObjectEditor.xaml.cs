using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Views;

namespace UAssetManager.Controls.Editors;
public partial class ObjectEditor : UserControl
{
    public static readonly DependencyProperty AssetProperty = DependencyProperty.Register(
        nameof(Asset), typeof(UAsset), typeof(ObjectEditor), new PropertyMetadata(null, OnAssetChanged));

    public static readonly DependencyProperty CurrentIndexProperty = DependencyProperty.Register(
        nameof(CurrentIndex), typeof(int), typeof(ObjectEditor), new PropertyMetadata(0, OnCurrentIndexChanged));

    public UAsset? Asset
    {
        get => (UAsset?)GetValue(AssetProperty);
        set => SetValue(AssetProperty, value);
    }

    public int CurrentIndex
    {
        get => (int)GetValue(CurrentIndexProperty);
        set => SetValue(CurrentIndexProperty, value);
    }

    private static void OnAssetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ObjectEditor)d;
        editor.UpdatePathDisplay();
    }

    private static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (ObjectEditor)d;
        editor.UpdateFromCurrentIndex();
    }

    public ObjectEditor()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateFromCurrentIndex();
    }

    private void UpdateFromCurrentIndex()
    {
        IndexTextBox.Text = CurrentIndex.ToString();
        UpdatePathDisplay();
    }

    private void IndexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(IndexTextBox.Text.Trim(), out int index))
        {
            CurrentIndex = index;
        }
    }

    private void UpdatePathDisplay()
    {
        if (Asset == null)
        {
            PathTextBlock.Text = "";
            return;
        }

        try
        {
            PathTextBlock.Text = ObjectReferenceDialog.GetFullPathFromPackageIndex(Asset, new FPackageIndex(CurrentIndex));
        }
        catch (Exception ex)
        {
            PathTextBlock.Text = $"Error: {ex.Message}";
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        if (Asset == null) return;

        var dialog = new ObjectReferenceDialog(Asset, CurrentIndex)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            CurrentIndex = dialog.SelectedIndex;
        }
    }
}

internal class ObjectPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new ObjectEditor
    {
        Asset = asset
    };

    public override DependencyProperty GetDependencyProperty() => ObjectEditor.CurrentIndexProperty;

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FPackageIndex index) return index.Index;
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index) return new FPackageIndex(index);
        return value;
    }
}