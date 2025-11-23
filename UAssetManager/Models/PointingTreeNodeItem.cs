using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetManager.Pak.Objects;
using UAssetManager.Resources;

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
/// <remarks>This class is used to organize and represent exports from a UAsset file in a hierarchical tree structure. 
/// It supports building child nodes dynamically based on the export's data and configuration settings.</remarks>
public class ExportPointingTreeNodeItem(UAsset asset, Export export)
    : PointingTreeNodeItem(GetName(asset, export), export)
{
    private bool _childrenBuilt = false;

    protected internal override void Materialize()
    {
        // build children if need
        if (_childrenBuilt || asset == null || Data == null) return;

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
                    export.ClassIndex.ToImport(asset).ObjectName.Value.Value :
                    export.ClassIndex.Index.ToString();

                var obj = UObject.CreateObject(normalExport);
                var parentNode = new PointingTreeNodeItem(className, obj, TreeNodeType.UObjectData);
                Children.Add(parentNode);

                if (!UAGConfig.Data.EnableDynamicTree)
                {
                    for (int j = 0; j < normalExport.Data.Count; j++)
                        InterpretThing(normalExport.Data[j], parentNode, !UAGConfig.Data.EnableDynamicTree);
                }

                // 处理特殊类型的Export
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

                // Properties
                foreach (var propertyData in obj.Data)
                {
                    var propertyNode = new PointingTreeNodeItem(propertyData.Name.ToString(), propertyData, TreeNodeType.UPropertyData);
                    parentNode.Children.Add(propertyNode);
                }

                // Fields
                foreach (var propertyData in obj.GetFields())
                {
                    var fieldNode = new PointingTreeNodeItem(propertyData.Name.ToString(), propertyData, TreeNodeType.UObjectField);
                    parentNode.Children.Add(fieldNode);
                }

                // Extra
                if (obj.Extras.Length > 0)
                {
                    var extrasNode = new PointingTreeNodeItem(
                        StringHelper.Get("Asset_BinaryNode", obj.Extras.Length), obj, TreeNodeType.ByteArray);
                    parentNode.Children.Add(extrasNode);
                }

                break;
            }
        }
    }

    private void AddOuterIndexChildren()
    {
        if (asset == null) return;

        var currentExportIndex = asset.Exports.IndexOf(export) + 1;
        foreach (var childExport in asset.Exports)
        {
            if (childExport.OuterIndex.Index == currentExportIndex)
            {
                var childNode = new ExportPointingTreeNodeItem(asset, childExport) { Type = TreeNodeType.SubExport };
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

    public static string GetName(UAsset asset, Export export)
    {
        var className = export.ClassIndex.IsNull() ? string.Empty : export.ClassIndex.IsImport() ?
            export.ClassIndex.ToImport(asset).ObjectName.ToString() :
            export.ClassIndex.ToExport(asset).ObjectName.ToString();

        return export.ObjectName.ToString() + "." + className;
    }
}

/// <summary>
/// Represents an entry in a dictionary that associates a key-value pair with an additional pointer object.
/// </summary>
/// <remarks>This class is useful for scenarios where a dictionary entry needs to be extended with additional
/// metadata or context, represented by the <see cref="Pointer"/> property.</remarks>
public class PointingDictionaryEntry(KeyValuePair<PropertyData, PropertyData> entry, object pointer)
{
    public KeyValuePair<PropertyData, PropertyData> Entry { get; set; } = entry;

    public object Pointer { get; set; } = pointer;
}