using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace UAssetManager.Models;
public partial class TreeNodeItem(string name, TreeNodeType type = TreeNodeType.Normal) : ObservableObject
{
    [ObservableProperty] string _name = name;
    [ObservableProperty] TreeNodeType _type = type;
    [ObservableProperty] bool _isExpanded = false;
    [ObservableProperty] bool _isSelected = false;
    [ObservableProperty] object? _data;
    [ObservableProperty] int _exportIndex = -1;
    [ObservableProperty] bool _hasChildren = true;
    [ObservableProperty] bool _isSearchMatched = false;
    [ObservableProperty] TreeNodeItem? _parent;
    [ObservableProperty] ObservableCollection<TreeNodeItem> _children = new();

    partial void OnChildrenChanged(ObservableCollection<TreeNodeItem>? oldValue, ObservableCollection<TreeNodeItem> newValue)
    {
        // Clear old child node parent references
        if (oldValue != null)
        {
            foreach (var child in oldValue)
            {
                child.Parent = null;
            }
        }

        // Set new child node parent references
        if (newValue != null)
        {
            foreach (var child in newValue)
            {
                child.Parent = this;
            }
        }
    }
    
    partial void OnIsExpandedChanged(bool value) => Materialize();
    partial void OnIsSelectedChanged(bool value) => Materialize();
    protected internal virtual void Materialize() { }
}

//<summary>
// Represents the type of the node in the tree view.
//</summary>
public enum TreeNodeType
{
    Normal = 0,
    GeneralInformation,
    NameMap,
    SoftObjectPathList,
    Imports,
    ExportInformation,
    SoftPackageReferences,
    DependsMap,
    WorldTileInfo,
    DataResources,
    CustomVersionContainer,
    ExportData,
    SubExport,

    // PointerNode
    StructData,
    ClassData,
    EnumData,
    UPropertyData,
    ByteArray,
    Dummy,
    UserDefinedStructData,
    Kismet,
    KismetByteArray,
    UObjectData,
    UObjectField,
}