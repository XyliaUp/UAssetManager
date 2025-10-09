using CommunityToolkit.Mvvm.ComponentModel;

namespace UAssetManager.Models;
public partial class PropertyItem(string name, object value, string type) : ObservableObject
{
    [ObservableProperty] string _name = name;
    [ObservableProperty] string _type = type;
    [ObservableProperty] string? _value = value?.ToString();

    public override string ToString() => $"{Name}: {Value}";

    public PropertyItem(string name, object value) : this(name, value, value?.GetType().Name ?? "null") { }
}