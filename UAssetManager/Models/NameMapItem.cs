using CommunityToolkit.Mvvm.ComponentModel;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Models;
public partial class NameMapItem(UAsset asset, int index) : ObservableObject
{
    private readonly UAsset _asset = asset;
    private readonly int _index = index;

	public int Index => _index;

    public string Name
    {
        get => _asset.GetNameReference(_index).Value;
        set
        {
            var currentFString = _asset.GetNameReference(_index);
            var newFString = new FString(value, currentFString.Encoding)
            {
                IsCasePreserving = currentFString.IsCasePreserving
            };
            _asset.SetNameReference(_index, newFString);
            OnPropertyChanged();
        }
    }

    public bool IsCasePreserving
    {
        get => _asset.GetNameReference(_index).IsCasePreserving;
        set
        {
            var currentFString = _asset.GetNameReference(_index);
            var newFString = new FString(currentFString.Value, currentFString.Encoding)
            {
                IsCasePreserving = value
            };
            _asset.SetNameReference(_index, newFString);
            OnPropertyChanged();
        }
    }

    public string Encoding
    {
        get => _asset.GetNameReference(_index).Encoding?.HeaderName ?? System.Text.Encoding.ASCII.HeaderName;
        set
        {
            var currentFString = _asset.GetNameReference(_index);
            var encoding = System.Text.Encoding.GetEncoding(value);
            var newFString = new FString(currentFString.Value, encoding)
            {
                IsCasePreserving = currentFString.IsCasePreserving
            };
            _asset.SetNameReference(_index, newFString);
            OnPropertyChanged();
        }
    }

    public bool HandlePropertyEdit(string? columnName, string newValue)
    {
        try
        {
            switch (columnName)
            {
                case nameof(Name):
                    Name = newValue;
                    return true;
                case nameof(IsCasePreserving):
                    IsCasePreserving = bool.Parse(newValue);
                    return true;
                case nameof(Encoding):
                    Encoding = newValue;
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
}