using System.Windows;
using System.Windows.Controls;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetManager.Controls.Editors;
internal class TextPropertyEditor : PropertyEditorBase
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox();

    public override DependencyProperty GetDependencyProperty() => TextBox.TextProperty;
}