using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI.PropertyTypes.Objects;
using UAssetManager.Controls.Editors;

namespace UAssetManager.Controls;
/// <summary>
/// Base class for all property editors that render and bind UI elements to PropertyData values.
/// Provides common binding helpers and sensible defaults.
/// </summary>
public abstract class PropertyEditorBase : DependencyObject
{
    /// <summary>
    /// Create a WPF element used to edit the given property.
    /// Implementations may call <see cref="CreateBinding(PropertyData, DependencyObject)"/> to wire bindings.
    /// </summary>
    /// <param name="property">The property to edit.</param>
    /// <returns>The editor element.</returns>
    public abstract FrameworkElement CreateElement(PropertyData property);

    /// <summary>
    /// Create and apply a binding from the given property to the specified element.
    /// Override to customize target dependency property or add multiple bindings.
    /// </summary>
    /// <param name="property">Source property to bind.</param>
    /// <param name="element">Target element that receives the binding.</param>
    public virtual void CreateBinding(PropertyData property, DependencyObject element)
    {
        var binding = new Binding("RawValue")
        {
            Source = property,
            Mode = GetBindingMode(property),
            UpdateSourceTrigger = GetUpdateSourceTrigger(property),
            Converter = GetConverter(),
            ConverterParameter = property,
        };

        // Add binding error handling
        binding.ValidationRules.Add(new BindingValidationRule());
        BindingOperations.SetBinding(element, GetDependencyProperty(), binding);
    }

    /// <summary>
    /// The dependency property used for the default binding (e.g., TextBox.TextProperty).
    /// </summary>
    public virtual DependencyProperty GetDependencyProperty() => TextBox.TextProperty;

    /// <summary>
    /// Determine binding mode for the given property. Defaults to TwoWay.
    /// </summary>
    public virtual BindingMode GetBindingMode(PropertyData property) => BindingMode.TwoWay;

    /// <summary>
    /// Determine update trigger for the binding. Defaults to PropertyChanged.
    /// </summary>
    public virtual UpdateSourceTrigger GetUpdateSourceTrigger(PropertyData property) => UpdateSourceTrigger.PropertyChanged;

    /// <summary>
    /// Optional value converter used by the binding. Return null if not needed.
    /// </summary>
    public virtual IValueConverter? GetConverter() => null;
}