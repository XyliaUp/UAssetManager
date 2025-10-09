using CommunityToolkit.Mvvm.ComponentModel;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Models;
public partial class ExportItem(UAsset asset, Export export) : ObservableObject
{
#pragma warning disable CS8625
    public ExportItem() : this(null, null) { }  // 用于DataGrid能显示新行
#pragma warning restore CS8625 

    internal Export Model => export;

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

    public string ObjectName
    {
        get => export.ObjectName?.ToString() ?? string.Empty;
        set
        {
            export.ObjectName = FName.FromString(asset, value);
            OnPropertyChanged();
        }
    }

    public int OuterIndex
    {
        get => export.OuterIndex.Index;
        set
        {
            export.OuterIndex = new FPackageIndex(value);
            OnPropertyChanged();
        }
    }

    public int ClassIndex
    {
        get => export.ClassIndex.Index;
        set
        {
            export.ClassIndex = new FPackageIndex(value);
            OnPropertyChanged();
        }
    }

    public int SuperIndex
    {
        get => export.SuperIndex.Index;
        set
        {
            export.SuperIndex = new FPackageIndex(value);
            OnPropertyChanged();
        }
    }

    public int TemplateIndex
    {
        get => export.TemplateIndex.Index;
        set
        {
            export.TemplateIndex = new FPackageIndex(value);
            OnPropertyChanged();
        }
    }

    public string ObjectFlags
    {
        get => export.ObjectFlags.ToString();
        set
        {
            if (Enum.TryParse<EObjectFlags>(value, out var flags)) export.ObjectFlags = flags;
            OnPropertyChanged();
        }
    }

    public string PackageFlags
    {
        get => export.PackageFlags.ToString();
        set
        {
            if (Enum.TryParse<EPackageFlags>(value, out var flags)) export.PackageFlags = flags;
            OnPropertyChanged();
        }
    }

    public bool bNotForClient
    {
        get => export.bNotForClient;
        set
        {
            export.bNotForClient = value;
            OnPropertyChanged();
        }
    }

    public bool bNotForServer
    {
        get => export.bNotForServer;
        set
        {
            export.bNotForServer = value;
            OnPropertyChanged();
        }
    }

    public bool bForcedExport
    {
        get => export.bForcedExport;
        set
        {
            export.bForcedExport = value;
            OnPropertyChanged();
        }
    }

    public bool bIsAsset
    {
        get => export.bIsAsset;
        set
        {
            export.bIsAsset = value;
            OnPropertyChanged();
        }
    }

    public bool HandlePropertyEdit(string? columnName, string newValue)
    {
        switch (columnName)
        {
            case nameof(ObjectName): ObjectName = newValue; return true;
            case nameof(OuterIndex): OuterIndex = int.Parse(newValue); return true;
            case nameof(ClassIndex): ClassIndex = int.Parse(newValue); return true;
            case nameof(SuperIndex): SuperIndex = int.Parse(newValue); return true;
            case nameof(TemplateIndex): TemplateIndex = int.Parse(newValue); return true;
            case nameof(ObjectFlags): ObjectFlags = newValue; return true;
            case nameof(PackageFlags): PackageFlags = newValue; return true;
            case nameof(bNotForClient): bNotForClient = bool.Parse(newValue); return true;
            case nameof(bNotForServer): bNotForServer = bool.Parse(newValue); return true;
            case nameof(bForcedExport): bForcedExport = bool.Parse(newValue); return true;
            case nameof(bIsAsset): bIsAsset = bool.Parse(newValue); return true;
            default: return false;
        }
    }
}