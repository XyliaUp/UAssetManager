using CommunityToolkit.Mvvm.ComponentModel;
using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Models;
public partial class ImportItem(UAsset asset, Import import) : ObservableObject
{
#pragma warning disable CS8625
    public ImportItem() : this(null, null) { }   // 用于DataGrid能显示新行
#pragma warning restore CS8625 

    internal Import Model => import;

    private int _index;
    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                OnPropertyChanged();
            }
        }
    }

    public string ClassPackage
    {
        get => import.ClassPackage.ToString();
        set
        {
            import.ClassPackage = FName.FromString(asset, value);
            OnPropertyChanged();
        }
    }

    public string ClassName
    {
        get => import.ClassName.ToString();
        set
        {
            import.ClassName = FName.FromString(asset, value);
            OnPropertyChanged();
        }
    }

    public int OuterIndex
    {
        get => import.OuterIndex.Index;
        set
        {
            import.OuterIndex = new FPackageIndex(value);
            OnPropertyChanged();
        }
    }

    public string ObjectName
    {
        get => import.ObjectName.ToString();
        set
        {
            import.ObjectName = FName.FromString(asset, value);
            OnPropertyChanged();
        }
    }

    public bool bImportOptional
    {
        get => import.bImportOptional;
        set
        {
            import.bImportOptional = value;
            OnPropertyChanged();
        }
    }

    public bool HandlePropertyEdit(string? columnName, string newValue)
    {
        switch (columnName)
        {
            case nameof(ClassPackage): ClassPackage = newValue; return true;
            case nameof(ClassName): ClassName = newValue; return true;
            case nameof(ObjectName): ObjectName = newValue; return true;
            case nameof(OuterIndex): OuterIndex = int.Parse(newValue); return true;
            case nameof(bImportOptional): bImportOptional = bool.Parse(newValue); return true;
            default: return false;
        }
    }
}