using CommunityToolkit.Mvvm.ComponentModel;

namespace UAssetManager.Models;
public partial class NameMapItem : ObservableObject
{
    [ObservableProperty] int _index;
    [ObservableProperty] string _name = string.Empty;
    [ObservableProperty] bool _isCasePreserving;
    [ObservableProperty] string _encoding = string.Empty;
}