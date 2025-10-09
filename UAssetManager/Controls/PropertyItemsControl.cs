using System.Windows;
using System.Windows.Controls;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetManager.Controls.Editors;

namespace UAssetManager.Controls;
public class PropertyItemsControl : ListBox
{
    protected override bool IsItemItsOwnContainerOverride(object item) => item is PropertyItem;

    public PropertyItemsControl()
    {
        VirtualizingPanel.SetIsVirtualizingWhenGrouping(this, true);
        VirtualizingPanel.SetScrollUnit(this, ScrollUnit.Pixel);
    }
}

public class PropertyItem : ListBoxItem
{
    #region Properties
    public static readonly DependencyProperty PropertyNameProperty = DependencyProperty.Register(nameof(PropertyName), typeof(string), typeof(PropertyItem));
    public string PropertyName
    {
        get => (string)GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(PropertyData), typeof(PropertyItem));
    public PropertyData Value
    {
        get => (PropertyData)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(PropertyItem));
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(nameof(Category), typeof(string), typeof(PropertyItem));
    public string Category
    {
        get => (string)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    public static readonly DependencyProperty EditorProperty = DependencyProperty.Register(nameof(Editor), typeof(PropertyEditorBase), typeof(PropertyItem));
    public PropertyEditorBase Editor
    {
        get => (PropertyEditorBase)GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public static readonly DependencyProperty EditorElementProperty = DependencyProperty.Register(nameof(EditorElement), typeof(FrameworkElement), typeof(PropertyItem));
    public FrameworkElement EditorElement
    {
        get => (FrameworkElement)GetValue(EditorElementProperty);
        set => SetValue(EditorElementProperty, value);
    }
    #endregion

    #region Data
    public static readonly DependencyProperty AssetProperty = DependencyProperty.Register(
        nameof(Asset), typeof(UAsset), typeof(PropertyItem), new PropertyMetadata(default(UAsset)));

    public static readonly DependencyProperty PropertyProperty = DependencyProperty.Register(
        nameof(Property), typeof(PropertyData), typeof(PropertyItem), new PropertyMetadata(null, OnPropertyChanged));

    public UAsset Asset
    {
        get => (UAsset?)GetValue(AssetProperty) ?? throw new ArgumentNullException(nameof(Asset));
        set => SetValue(AssetProperty, value);
    }

    public PropertyData Property
    {
        get => (PropertyData)GetValue(PropertyProperty);
        set => SetValue(PropertyProperty, value);
    }
    #endregion

    #region Methods
    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (PropertyItem)d;
        ctl.OnPropertyChanged((PropertyData)e.NewValue);
    }

    private void OnPropertyChanged(PropertyData property)
    {
        Value = property;
        PropertyName = property.Name.ToString();
        Description = GetType(property);

        Editor = ResolveEditor(Asset, property);
        EditorElement = Editor.CreateElement(property);
        Editor.CreateBinding(property, EditorElement);
    }

    private static string GetType(PropertyData property) => property switch
    {
        BytePropertyData bp when bp.ByteType == BytePropertyType.FName => bp.EnumType.ToString(),
        _ => property.PropertyType.Value,
    };

    public static PropertyEditorBase ResolveEditor(UAsset asset, PropertyData property) => property switch
    {
        ArrayPropertyData => new ArrayPropertyEditor(asset),
        StructPropertyData => new StructPropertyEditor(asset),
        BytePropertyData => new BytePropertyEditor(asset),
        EnumPropertyData => new EnumPropertyEditor(asset),
        ObjectPropertyData => new ObjectPropertyEditor(asset),

        BoolPropertyData => new BoolPropertyEditor(),
        IntPropertyData => new IntPropertyEditor(),
        FloatPropertyData => new FloatPropertyEditor(),
        DoublePropertyData => new FloatPropertyEditor(),
        StrPropertyData => new StrPropertyEditor(),
        NamePropertyData => new NamePropertyEditor(),
        VectorPropertyData => new VectorPropertyEditor(),
        Vector2DPropertyData => new Vector2DPropertyEditor(),
        Vector4PropertyData => new Vector4PropertyEditor(),
        RotatorPropertyData => new RotatorPropertyEditor(),
        LinearColorPropertyData => new LinearColorPropertyEditor(),
        ColorPropertyData => new ColorPropertyEditor(),
        QuatPropertyData => new QuatPropertyEditor(),
        _ => new ReadOnlyTextPropertyEditor()
    };
    #endregion
}