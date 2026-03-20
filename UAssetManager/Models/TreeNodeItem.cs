using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAssetManager.Models;

public partial class TreeNodeItem : ObservableCollection<TreeNodeItem>
{
	#region Properties
	public TreeNodeItem? Parent;

	private string _name = string.Empty;
	public string Name { get => _name; set => SetProperty(ref _name, value); }

	private TreeNodeType _type;
	public TreeNodeType Type { get => _type; set => SetProperty(ref _type, value); }

	private bool _isExpanded;
	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			SetProperty(ref _isExpanded, value);
			Materialize();
		}
	}

	private bool _isSelected;
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			SetProperty(ref _isSelected, value);
			Materialize();
		}
	}

	private bool _isSearchMatched;
	public bool IsSearchMatched { get => _isSearchMatched; set => SetProperty(ref _isSearchMatched, value); }

	private object? _data;
	public object? Data { get => _data; set => SetProperty(ref _data, value); }

	protected virtual bool DynamicMaterialize => true;
	public Collection<TreeNodeItem> Children => this;
	#endregion

	#region Methods
	public TreeNodeItem(string name, TreeNodeType type = TreeNodeType.Normal)
	{
		CollectionChanged += OnCollectionChanged;
		Name = name;
		Type = type;

		if (!DynamicMaterialize) Materialize();
	}

	private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		// Clear old child node parent references
		if (e.OldItems != null)
		{
			foreach (TreeNodeItem child in e.OldItems)
			{
				child.Parent = null;
			}
		}

		// Set new child node parent references
		if (e.NewItems != null)
		{
			foreach (TreeNodeItem child in e.NewItems)
			{
				child.Parent = this;
			}
		}
	}

	protected internal virtual void Materialize() { }

	protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(storage, value))
			return false;

		storage = value;
		OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		return true;
	}
	#endregion
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