using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet.Bytecode;
using UAssetAPI.Kismet.Bytecode.Expressions;
using UAssetAPI.Pak.Objects;
using UAssetAPI.Pak.Utils;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
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
/// Represents an entry in a dictionary that associates a key-value pair with an additional pointer object.
/// </summary>
/// <remarks>This class is useful for scenarios where a dictionary entry needs to be extended with additional
/// metadata or context, represented by the <see cref="Pointer"/> property.</remarks>
public class PointingDictionaryEntry(KeyValuePair<PropertyData, PropertyData> entry, object pointer)
{
	public KeyValuePair<PropertyData, PropertyData> Entry { get; set; } = entry;

	public object Pointer { get; set; } = pointer;
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

		switch (export)
		{
			case RawExport raw:
			{
				var parentNode = new PointingTreeNodeItem("Raw Data (" + raw.Data.Length + " B)", raw, TreeNodeType.ByteArray);
				Children.Add(parentNode);
				break;
			}
			case DataTableExport export:
			{
				foreach (var propertyData in export.Table.Data)
				{
					var propertyNode = new PointingTreeNodeItem(propertyData.Name.ToString(), propertyData, TreeNodeType.UserDefinedStructData);
					Children.Add(propertyNode);
				}
				break;
			}
			case NormalExport export:
			{
				if (export is ClassExport classExport)
				{
					var funcMapNode = new PointingTreeNodeItem("FuncMap", classExport.FuncMap, TreeNodeType.UserDefinedStructData);
					foreach (var (name, target) in classExport.FuncMap)
					{
						funcMapNode.Children.Add(new PointingTreeNodeItem(name.ToString(), target, TreeNodeType.SoftPackageReferences));
					}

					Children.Add(funcMapNode);
				}

				if (export is StringTableExport stringTableExport)
				{
					var parentNode2 = new PointingTreeNodeItem(
						(stringTableExport.Table?.TableNamespace?.ToString() ?? FString.NullCase) + " (" + stringTableExport.Table.Count + ")",
						stringTableExport.Table,
						TreeNodeType.Normal);
					Children.Add(parentNode2);
				}

				if (export is StructExport structExport)
				{
					if (structExport.ScriptBytecode == null)
					{
						var bytecodeNode = new PointingTreeNodeItem("ScriptBytecode (" + structExport.ScriptBytecodeRaw.Length + " B)", structExport, TreeNodeType.KismetByteArray);
						Children.Add(bytecodeNode);
					}
					else
					{
						var bytecodeNode = new PointingTreeNodeItem("ScriptBytecode (" + structExport.ScriptBytecode.Length + " instructions)", structExport, TreeNodeType.Kismet);
						Children.Add(bytecodeNode);

						foreach (var code in structExport.ScriptBytecode)
						{
							var codeNode = new ExpressionPointingTreeNodeItem(code);
							bytecodeNode.Children.Add(codeNode);
						}
					}
				}

				if (export is UserDefinedStructExport userDefinedStructExport)
				{
					var parentNode2 = new PointingTreeNodeItem("UserDefinedStruct Data (" + userDefinedStructExport.StructData.Count + ")", export, TreeNodeType.UserDefinedStructData);
					Children.Add(parentNode2);
				}

				var obj = UObject.CreateObject(export);
				if (UAGConfig.Data.EnableDynamicTree)
				{
					// Properties
					foreach (var propertyData in export.Data)
					{
						var propertyNode = new PointingTreeNodeItem(propertyData.Name.ToString(), propertyData, TreeNodeType.UPropertyData);
						Children.Add(propertyNode);
					}

					// Fields
					if (obj != null)
					{
						foreach (var propertyData in obj.GetFields())
						{
							var fieldNode = new PointingTreeNodeItem(propertyData.Name.ToString(), propertyData, TreeNodeType.UObjectField);
							Children.Add(fieldNode);
						}
					}
				}

				// Extra
				if (obj != null && obj.Extras.Length > 0)
				{
					var extrasNode = new PointingTreeNodeItem(StringHelper.Get("Asset_BinaryNode", obj.Extras.Length), obj, TreeNodeType.ByteArray);
					Children.Add(extrasNode);
				}
				break;
			}
		}

		// if outer index tree mode is enabled, add children organized by OuterIndex first
		AddOuterIndexChildren();
	}

	private void AddOuterIndexChildren()
	{
		if (asset == null || !UAGConfig.Data.EnableOuterTree) return;

		var index = asset.Exports.IndexOf(export) + 1;
		var classType = export.GetExportClassType().ToString();

		// WidgetTree firstly
		var children = new HashSet<int>();
		if (string.Equals(classType, "WidgetTree", StringComparison.OrdinalIgnoreCase))
		{
			var RootWidget = export.Find<ObjectPropertyData>("RootWidget");
			if (RootWidget != null) children.Add(RootWidget.Value.Index);
		}
		else if (classType.EndsWith("Slot", StringComparison.OrdinalIgnoreCase))
		{
			var Content = export.Find<ObjectPropertyData>("Content");
			if (Content != null) children.Add(Content.Value.Index);
		}

		for (int i = 0; i < asset.Exports.Count; i++)
		{
			var current = asset.Exports[i];
			if (children.Count > 0 ? !children.Contains(i + 1) : current.OuterIndex.Index != index) continue;

			var childNode = new ExportPointingTreeNodeItem(asset, current) { Type = TreeNodeType.SubExport };
			childNode.Add(new TreeNodeItem("loading...", TreeNodeType.Dummy));
			Add(childNode);
		}
	}

	private static void InterpretThing(PropertyData property, TreeNodeItem parentNode, bool fillAllSubNodes)
	{
		if (property == null) return;

		switch (property)
		{
			case NiagaraVariableBasePropertyData niagaraProp:
				InterpretThing(niagaraProp.TypeDef, parentNode, fillAllSubNodes);
				break;
			case GameplayTagContainerPropertyData gameplayTagProp:
				parentNode.Add(new PointingTreeNodeItem(
					string.Format("{0} ({1})", gameplayTagProp.Name, gameplayTagProp.Value.Length), gameplayTagProp));
				break;
			case MulticastDelegatePropertyData multicastDelegateProp:
				parentNode.Add(new PointingTreeNodeItem(
					string.Format("{0} ({1})", multicastDelegateProp.Name, multicastDelegateProp.Value.Length), multicastDelegateProp.Value));
				break;

			case BoxPropertyData boxProp:
				parentNode.Add(new PointingTreeNodeItem(boxProp.Name?.Value.Value + " (2)", boxProp));
				break;
			case Box2DPropertyData box2DProp:
				parentNode.Add(new PointingTreeNodeItem(box2DProp.Name?.Value.Value + " (2)", box2DProp));
				break;
			case Box2fPropertyData box2fProp:
				parentNode.Add(new PointingTreeNodeItem(box2fProp.Name?.Value.Value + " (2)", box2fProp));
				break;

			case ArrayPropertyData arrayProp:
				var arrNode = new PointingTreeNodeItem(string.Format("{0} ({1})", arrayProp.Name, arrayProp.Value.Length), arrayProp);
				parentNode.Add(arrNode);

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
				parentNode.Add(mapNode);

				if (fillAllSubNodes)
				{
					int index = 0;
					foreach (var kvp in mapProp.Value)
					{
						var dictEntry = new PointingDictionaryEntry(kvp, mapProp);
						mapNode.Add(new PointingTreeNodeItem("[" + index + "]", dictEntry));
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
				parentNode.Add(structNode);

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
/// Represents a tree node item that points to an expression
/// </summary>
public class ExpressionPointingTreeNodeItem(KismetExpression expression)
	: TreeNodeItem(expression.Inst, TreeNodeType.Kismet)
{
	private bool _childrenBuilt = false;

	protected override bool DynamicMaterialize => false;

	protected internal override void Materialize()
	{
		// build children if need
		if (_childrenBuilt || expression == null) return;

		_childrenBuilt = true;
		Children.Clear();

		switch (expression)
		{
			case EX_FinalFunction function:
				foreach (var param in function.Parameters) Children.Add(new ExpressionPointingTreeNodeItem(param));
				break;

			case EX_StructConst function:
				foreach (var param in function.Value) Children.Add(new ExpressionPointingTreeNodeItem(param));
				break;

			case EX_Jump function:
				Name += $" ({function.CodeOffset})";
				break;

			default:
				if (IsGenericKismetExpression(expression.GetType())) Name += $" ({expression.RawValue})";
				break;
		}
	}

	private static bool IsGenericKismetExpression(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		var currentType = type.BaseType;
		while (currentType != null)
		{
			if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(KismetExpression<>))
			{
				return true;
			}
			currentType = currentType.BaseType;
		}
		return false;
	}
}