using System.Windows;
using System.Windows.Controls;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetManager.Controls.Editors;
internal class ReadOnlyTextPropertyEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox() 
    {
        IsReadOnly = true 
    };

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;
}