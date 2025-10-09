using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Models;

/// <summary>
/// Represents a tree node item that includes an optional pointer to associated data.
/// </summary>
/// <remarks>This class extends <see cref="TreeNodeItem"/> by allowing the association of an additional object, 
/// referred to as a pointer, which can store supplementary data or context for the node.</remarks>
public partial class PointingTreeNodeItem : TreeNodeItem
{
    public PointingTreeNodeItem(string name, object? pointer, TreeNodeType type = TreeNodeType.Normal) : base(name, type)
    {
        Data = pointer;
    }
}

/// <summary>
/// Represents a tree node item that points to an export within a UAsset file.
/// </summary>
/// <remarks>This class is used to organize and represent exports from a UAsset file in a hierarchical tree
/// structure. It supports building child nodes dynamically based on the export's data and configuration
/// settings.</remarks>
/// <param name="asset"></param>
/// <param name="export"></param>
public class ExportPointingTreeNodeItem(UAsset asset, Export export) :
    PointingTreeNodeItem(export.ObjectName.ToString(), export)
{
    private readonly UAsset? _asset = asset;
    private bool _childrenBuilt = false;

    protected internal override void Materialize()
    {
        // build children if need
        if (_childrenBuilt || _asset == null || Data == null) return;

        _childrenBuilt = true;
        Children.Clear();

        // if outer index tree mode is enabled, add children organized by OuterIndex first
        if (UAGConfig.Data.UseOuterIndexTreeMode)
        {
            AddOuterIndexChildren();
        }

        switch (export)
        {
            case RawExport rawExport:
            {
                var parentNode = new PointingTreeNodeItem("Raw Data (" + rawExport.Data.Length + " B)", rawExport, TreeNodeType.ByteArray);
                Children.Add(parentNode);
                break;
            }
            case NormalExport normalExport:
            {
                string className = export.ClassIndex.IsImport() ?
                    export.ClassIndex.ToImport(_asset).ObjectName.Value.Value :
                    export.ClassIndex.Index.ToString();
                var parentNode = new PointingTreeNodeItem(className + " (" + normalExport.Data.Count + ")", normalExport);
                Children.Add(parentNode);

                if (!UAGConfig.Data.EnableDynamicTree)
                {
                    for (int j = 0; j < normalExport.Data.Count; j++)
                        InterpretThing(normalExport.Data[j], parentNode, !UAGConfig.Data.EnableDynamicTree);
                }


                if (normalExport is StringTableExport stringTableExport)
                {
                    var parentNode2 = new PointingTreeNodeItem(
                        (stringTableExport.Table?.TableNamespace?.ToString() ?? FString.NullCase) + " (" + stringTableExport.Table.Count + ")",
                        stringTableExport.Table,
                        TreeNodeType.Normal);
                    Children.Add(parentNode2);
                }

                if (normalExport is StructExport structExport)
                {
                    var parentNode2 = new PointingTreeNodeItem("UStruct Data", structExport, TreeNodeType.StructData);
                    Children.Add(parentNode2);

                    if (structExport.ScriptBytecode == null)
                    {
                        var bytecodeNode = new PointingTreeNodeItem("ScriptBytecode (" + structExport.ScriptBytecodeRaw.Length + " B)", structExport, TreeNodeType.KismetByteArray);
                        parentNode2.Children.Add(bytecodeNode);
                    }
                    else
                    {
                        var bytecodeNode = new PointingTreeNodeItem("ScriptBytecode (" + structExport.ScriptBytecode.Length + " instructions)", structExport, TreeNodeType.Kismet);
                        parentNode2.Children.Add(bytecodeNode);
                    }
                }

                if (normalExport is UserDefinedStructExport userDefinedStructExport)
                {
                    var parentNode2 = new PointingTreeNodeItem("UserDefinedStruct Data (" + userDefinedStructExport.StructData.Count + ")", normalExport, TreeNodeType.UserDefinedStructData);
                    Children.Add(parentNode2);
                }

                if (normalExport.Extras != null && normalExport.Extras.Length > 0)
                {
                    var extrasNode = new PointingTreeNodeItem("Extras (" + normalExport.Extras.Length + " B)", normalExport, TreeNodeType.ByteArray);
                    Children.Add(extrasNode);
                }
                break;
            }
        }
    }

    private void AddOuterIndexChildren()
    {
        if (_asset == null) return;

        var currentExportIndex = _asset.Exports.IndexOf(export) + 1;
        foreach (var childExport in _asset.Exports)
        {
            if (childExport.OuterIndex.Index == currentExportIndex)
            {
                var childNode = new ExportPointingTreeNodeItem(_asset, childExport) { Type = TreeNodeType.SubExport };              
                var loadingNode = new TreeNodeItem("loading...", TreeNodeType.Dummy);
                childNode.Children.Add(loadingNode);
                Children.Add(childNode);
            }
        }
    }

    private static void InterpretThing(PropertyData property, PointingTreeNodeItem parentNode, bool fillAllSubNodes)
    {
        if (property == null) return;

        switch (property)
        {
            case NiagaraVariableBasePropertyData niagaraProp:
                InterpretThing(niagaraProp.TypeDef, parentNode, fillAllSubNodes);
                break;
            case GameplayTagContainerPropertyData gameplayTagProp:
                parentNode.Children.Add(new PointingTreeNodeItem(
                    string.Format("{0} ({1})", gameplayTagProp.Name, gameplayTagProp.Value.Length), gameplayTagProp));
                break;
            case MulticastDelegatePropertyData multicastDelegateProp:
                parentNode.Children.Add(new PointingTreeNodeItem(
                    string.Format("{0} ({1})", multicastDelegateProp.Name, multicastDelegateProp.Value.Length), multicastDelegateProp.Value));
                break;

            case BoxPropertyData boxProp:
                parentNode.Children.Add(new PointingTreeNodeItem(boxProp.Name?.Value.Value + " (2)", boxProp));
                break;
            case Box2DPropertyData box2DProp:
                parentNode.Children.Add(new PointingTreeNodeItem(box2DProp.Name?.Value.Value + " (2)", box2DProp));
                break;
            case Box2fPropertyData box2fProp:
                parentNode.Children.Add(new PointingTreeNodeItem(box2fProp.Name?.Value.Value + " (2)", box2fProp));
                break;

            case ArrayPropertyData arrayProp:
                var arrNode = new PointingTreeNodeItem(string.Format("{0} ({1})", arrayProp.Name, arrayProp.Value.Length), arrayProp);
                parentNode.Children.Add(arrNode);

                if (fillAllSubNodes)
                {
                    for (int j = 0; j < arrayProp.Value.Length; j++)
                    {
                        InterpretThing(arrayProp.Value[j], arrNode, fillAllSubNodes);
                    }
                }

                break;
            case MapPropertyData mapProp:
                var mapNode = new PointingTreeNodeItem(string.Format("{0} ({1})", mapProp.Name, mapProp.Value.Count), mapProp);
                parentNode.Children.Add(mapNode);

                if (fillAllSubNodes)
                {
                    int index = 0;
                    foreach (var kvp in mapProp.Value)
                    {
                        var dictEntry = new PointingDictionaryEntry(kvp, mapProp);
                        mapNode.Children.Add(new PointingTreeNodeItem("[" + index + "]", dictEntry));
                        index++;
                    }
                }
                break;
            case StructPropertyData structProp:
                string decidedName = structProp.Name.ToString();
                if (parentNode.Data is PropertyData data && data.Name.ToString() == decidedName)
                {
                    decidedName = structProp.StructType.ToString();
                }
                var structNode = new PointingTreeNodeItem(decidedName, structProp);
                parentNode.Children.Add(structNode);

                if (fillAllSubNodes)
                {
                    for (int j = 0; j < structProp.Value.Count; j++)
                    {
                        InterpretThing(structProp.Value[j], structNode, fillAllSubNodes);
                    }
                }
                break;
        }
    }
}

/// <summary>
/// Represents an entry in a dictionary that associates a key-value pair with an additional pointer object.
/// </summary>
/// <remarks>This class is useful for scenarios where a dictionary entry needs to be extended with additional
/// metadata or context, represented by the <see cref="Pointer"/> property.</remarks>
public class PointingDictionaryEntry
{
    public KeyValuePair<PropertyData, PropertyData> Entry { get; set; }
    public object Pointer { get; set; }

    public PointingDictionaryEntry(KeyValuePair<PropertyData, PropertyData> entry, object pointer)
    {
        Entry = entry;
        Pointer = pointer;
    }
}