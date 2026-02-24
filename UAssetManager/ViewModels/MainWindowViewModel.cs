using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiscordRPC;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.Kismet;
using UAssetAPI.Pak.Pak.Utils;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;
using UAssetManager.Models;
using UAssetManager.Resources;
using UAssetManager.Utils;
using UAssetManager.Views;

namespace UAssetManager.ViewModels;

public partial class MainWindowViewModel : ObservableObject, ITreeSearchProvider, IPluginHost
{
	#region Events
	public static event EventHandler<AssetUpdateEventArgs>? AssetUpdateRequested;
	#endregion

	#region Properties
	[ObservableProperty] ObservableCollection<string> _mappings = new();
	[ObservableProperty] int _selectedMapping;
	[ObservableProperty] string _status = StringHelper.Get("MainWindow_Status.Ready");

	[ObservableProperty] string _currentFilePath = string.Empty;
	[ObservableProperty] UAsset? _currentAsset;
	[ObservableProperty] ObservableCollection<TreeNodeItem> _treeNodes = new();
	[ObservableProperty] ObservableCollection<CustomVersionItem> _customVersionItems = new();
	[ObservableProperty] ObservableCollection<NameMapItem> _nameMapItems = new();
	[ObservableProperty] ObservableCollection<ImportItem> _importItems = new();
	[ObservableProperty] ObservableCollection<ExportItem> _exportItems = new();
	[ObservableProperty] ObservableCollection<PropertyItem> _propertyItems = new();
	[ObservableProperty] TreeNodeItem? _selectedTreeNode;
	[ObservableProperty] object? _currentDataContext;
	[ObservableProperty] bool _hasUnsavedChanges = false;
	[ObservableProperty] bool _canAddRows = false;
	[ObservableProperty] bool _canDeleteRows = false;
	[ObservableProperty] bool _isRawDataSelected = false;
	[ObservableProperty] bool _isAssetFromBuildPak = false;

	private PluginLoader? _pluginLoader;
	public IReadOnlyCollection<IUAssetPlugin> Plugins => _pluginLoader?.Plugins ?? Array.Empty<IUAssetPlugin>();

	public string DisplayVersion => $"UAssetManager v{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}";

	public IEnumerable<EngineVersion> EngineVersions => Enum.GetValues<EngineVersion>();

	public EngineVersion SelectedEngineVersion
	{
		get => UAGConfig.Data.PreferredVersion;
		set
		{
			UAGConfig.Data.PreferredVersion = value;
			OnPropertyChanged();
		}
	}
	#endregion

	#region Constructor
	public MainWindowViewModel()
	{
		// Initialize mapping list
		Mappings.Add(StringHelper.Get("MainWindow_NoMapping"));
		Mappings.Add(StringHelper.Get("MainWindow_CustomMapping"));

		// Initialize plugin system
		InitializePluginSystem();
	}

	#endregion

	#region Commands

	[RelayCommand] void OpenUrl(string url) => UAGUtils.OpenUrl(url);

	[RelayCommand] void OpenContainer() => new FileContainerWindow().Show();

	[RelayCommand] void Find() => new FindWindow() { Owner = Application.Current.MainWindow }.ShowDialog();

	[RelayCommand] void MapStructOverrides() => new MapStructTypeOverrideWindow() { Owner = Application.Current.MainWindow }.ShowDialog();

	[RelayCommand] void OpenSettings() => new SettingsWindow() { Owner = Application.Current.MainWindow }.ShowDialog();

	[RelayCommand] void OpenAbout() => new AboutWindow() { Owner = Application.Current.MainWindow }.ShowDialog();

	[RelayCommand]
	void OpenFile()
	{
		var dlg = new OpenFileDialog
		{
			Title = StringHelper.Get("MainWindow_OpenUAssetTitle"),
			Filter = "UAsset Files (*.uasset)|*.uasset|JSON Files (*.json)|*.json|PAK Files (*.pak)|*.pak|All Files (*.*)|*.*",
			InitialDirectory = UAGConfig.Data.ExtractedFolder,
			Multiselect = false,
		};
		if (dlg.ShowDialog() == true)
		{
			UAGConfig.Data.ExtractedFolder = Path.GetDirectoryName(dlg.FileName);
			LoadFile(dlg.FileName);
		}
	}

	[RelayCommand]
	void Refresh()
	{
		if (!string.IsNullOrEmpty(CurrentFilePath) && File.Exists(CurrentFilePath))
		{
			try
			{
				CurrentAsset = new UAsset(CurrentFilePath, SelectedEngineVersion);
				HasUnsavedChanges = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_FileLoadErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		else
		{
			MessageBox.Show(StringHelper.Get("MainWindow_NoFilesToRefresh"), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}

	[RelayCommand]
	void Copy()
	{
		// TODO: copy export item
		var node = SelectedTreeNode;
	}

	[RelayCommand]
	void Paste()
	{

	}

	[RelayCommand]
	void ImportMappings()
	{
		var dlg = new OpenFileDialog
		{
			Title = StringHelper.Get("MainWindow_ImportMappingsTitle"),
			Filter = "USMAP Files (*.usmap)|*.usmap|All Files (*.*)|*.*",
			Multiselect = false
		};
		if (dlg.ShowDialog() == true)
		{
			try
			{
				var mappings = new UAssetAPI.Unversioned.Usmap(dlg.FileName);
				if (CurrentAsset != null) CurrentAsset.Mappings = mappings;
				MessageBox.Show(StringHelper.Get("MainWindow_MappingsImported", System.IO.Path.GetFileName(dlg.FileName)), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_MappingImportErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	[RelayCommand]
	void PatchUsmap()
	{
		var dlg = new OpenFileDialog
		{
			Title = StringHelper.Get("MainWindow_SelectSavTitle"),
			Filter = "Save Files (*.sav)|*.sav|All Files (*.*)|*.*",
			Multiselect = false
		};
		if (dlg.ShowDialog() == true)
		{
			try
			{
				var mappings = new UAssetAPI.Unversioned.Usmap(dlg.FileName);
				if (CurrentAsset != null) CurrentAsset.Mappings = mappings;
				MessageBox.Show(StringHelper.Get("MainWindow_UsmapPatched", System.IO.Path.GetFileName(dlg.FileName)), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_MappingImportErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	[RelayCommand]
	void DumpProperties()
	{
		if (CurrentAsset == null)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_NoFileToDumpMessage"), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}
		var dlg = new SaveFileDialog
		{
			Title = StringHelper.Get("MainWindow_SavePropertiesTitle"),
			Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
			DefaultExt = "txt"
		};
		if (dlg.ShowDialog() == true)
		{
			try
			{
				var json = CurrentAsset.SerializeJson();
				File.WriteAllText(dlg.FileName, json);
				MessageBox.Show(StringHelper.Get("MainWindow_PropertiesDumped", System.IO.Path.GetFileName(dlg.FileName)), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_PropertyDumpingErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	[RelayCommand]
	void OpenConfigDir()
	{
		var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "UAssetGUI");
		if (Directory.Exists(configPath))
		{
			UAGUtils.OpenFolder(configPath);
		}
		else
		{
			MessageBox.Show(StringHelper.Get("MainWindow_ConfigDirNotExistMessage"), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}

	[RelayCommand]
	void ReplaceAllReferences()
	{
		if (CurrentAsset == null)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_NoFileToReplaceMessage"), StringHelper.Get("MainWindow_InformationTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}

		var replaceDialog = new TextPromptWindow
		{
			Title = StringHelper.Get("MainWindow_ReplaceReferencesTitle"),
			DisplayText = StringHelper.Get("MainWindow_EnterTextToFind")
		};

		if (replaceDialog.ShowDialog() == true)
		{
			var findText = replaceDialog.OutputText;
			var replaceDialog2 = new TextPromptWindow
			{
				Title = StringHelper.Get("MainWindow_ReplaceReferencesTitle"),
				DisplayText = StringHelper.Get("MainWindow_EnterReplacementText")
			};

			if (replaceDialog2.ShowDialog() == true)
			{
				var replaceText = replaceDialog2.OutputText;
				try
				{
					var json = CurrentAsset.SerializeJson();
					var replaced = string.IsNullOrEmpty(findText) ? json : json.Replace(findText, replaceText);
					var newAsset = UAsset.DeserializeJson(new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(replaced)));
					newAsset.SetEngineVersion(SelectedEngineVersion);
					CurrentAsset = newAsset;
					HasUnsavedChanges = true;
					PopulateTreeView();
					Status = StringHelper.Get("MainWindow_Status.FileModified");
				}
				catch (Exception ex)
				{
					MessageBox.Show(StringHelper.Get("MainWindow_GenericErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
	}

	#endregion

	#region Load Methods

	partial void OnCurrentAssetChanged(UAsset? value)
	{
		if (value == null) return;

		PropertyItems.Clear();

		// Add general information
		PropertyItems.Add(new PropertyItem("File Path", CurrentFilePath));
		PropertyItems.Add(new PropertyItem("Engine Version", value.GetEngineVersion()));
		PropertyItems.Add(new PropertyItem("Export Count", value.Exports.Count));
		PropertyItems.Add(new PropertyItem("Import Count", value.Imports.Count));

		// Ensure the DataGrid has a data source
		CurrentDataContext = PropertyItems;
	}

	public void SetCurrentAsset(UAsset asset, string virtualPath, bool fromBuildPak)
	{
		try
		{
			CurrentAsset = asset;
			PopulateTreeView();
			IsAssetFromBuildPak = fromBuildPak;
			CurrentFilePath = virtualPath;
			HasUnsavedChanges = false;
			_lastOpenedTime = DateTime.Now;
			Status = $"Loaded: {Path.GetFileName(virtualPath)}";
			UpdateDiscordRpc();
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_AssetOpenErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	public void LoadFile(string filePath)
	{
		try
		{
			Status = "Loading file...";

			var fileExtension = Path.GetExtension(filePath).ToLower();
			switch (fileExtension)
			{
				case ".json":
					LoadJsonFile(filePath);
					break;
				case ".pak":
					LoadPakFile(filePath);
					break;
				default:
					LoadUAssetFile(filePath);
					break;
			}

			CurrentFilePath = filePath;
			HasUnsavedChanges = false;
			_lastOpenedTime = DateTime.Now;
			Status = $"Loaded: {Path.GetFileName(filePath)}";
			UpdateDiscordRpc();
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_FileLoadErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			Status = "Failed to load file";
		}
	}

	private void LoadUAssetFile(string filePath)
	{
		try
		{
			CurrentAsset = new UAsset(filePath, SelectedEngineVersion);
			PopulateTreeView();
			Status = $"UAsset file loaded successfully: {Path.GetFileName(filePath)}";
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_UAssetLoadErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			Status = "Failed to load UAsset file";
		}
	}

	private void LoadJsonFile(string filePath)
	{
		try
		{
			using var stream = new FileStream(filePath, FileMode.Open);
			CurrentAsset = UAsset.DeserializeJson(stream);
			CurrentAsset.Mappings = null;
			PopulateTreeView();
			Status = $"JSON file loaded successfully: {Path.GetFileName(filePath)}";
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_JsonLoadErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			Status = "Failed to load JSON file";
		}
	}

	public void LoadPakFile(string filePath)
	{
		try
		{
			var window = new FileContainerWindow(filePath);
			window.Show();
			Status = $"PAK file opened in File Container: {Path.GetFileName(filePath)}";
		}
		catch (Exception ex)
		{
			MessageBox.Show(StringHelper.Get("MainWindow_FileLoadErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			Status = "Failed to load PAK file";
		}
	}

	private void PopulateTreeView()
	{
		if (CurrentAsset == null) return;

		TreeNodes.Clear();
		TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_GeneralInformation"), TreeNodeType.GeneralInformation));

		if (CurrentAsset.SoftObjectPathList != null && (CurrentAsset.SoftObjectPathList.Count > 0 || !CurrentAsset.IsFilterEditorOnly))
			TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_SoftObjectPaths"), TreeNodeType.SoftObjectPathList));

		TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_NameMap"), TreeNodeType.NameMap));
		TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_ImportData"), TreeNodeType.Imports));
		TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_ExportInformation"), TreeNodeType.ExportInformation));

		if (CurrentAsset.DependsMap != null && CurrentAsset.DependsMap.Count > 0)
			TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_DependsMap"), TreeNodeType.DependsMap));

		if (CurrentAsset.SoftPackageReferenceList != null)
			TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_SoftPackageReferences"), TreeNodeType.SoftPackageReferences));

		if (CurrentAsset.WorldTileInfo != null)
			TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_WorldTileInfo"), TreeNodeType.WorldTileInfo));

		if (CurrentAsset.ObjectVersionUE5 >= ObjectVersionUE5.DATA_RESOURCES)
			TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_DataResources"), TreeNodeType.DataResources));

		TreeNodes.Add(new TreeNodeItem(StringHelper.Get("Asset_CustomVersionContainer"), TreeNodeType.CustomVersionContainer));

		var exportDataNode = new PointingTreeNodeItem(StringHelper.Get("Asset_ExportData"), null, TreeNodeType.ExportData) { IsExpanded = true };
		for (int i = 0; i < CurrentAsset.Exports.Count; i++)
		{
			var export = CurrentAsset.Exports[i];
			if (UAGConfig.Data.UseOuterIndexTreeMode && export.OuterIndex.IsExport()) continue;

			var categoryNode = new ExportPointingTreeNodeItem(CurrentAsset, export);
			var loadingNode = new TreeNodeItem("loading...", TreeNodeType.Dummy);
			categoryNode.Children.Add(loadingNode);
			exportDataNode.Children.Add(categoryNode);
		}

		TreeNodes.Add(exportDataNode);
	}


	partial void OnSelectedTreeNodeChanged(TreeNodeItem? value)
	{
		if (value == null || CurrentAsset == null) return;

		if (value is PointingTreeNodeItem pNode)
		{
			IsRawDataSelected = pNode.Type == TreeNodeType.ByteArray;
		}
		else
		{
			IsRawDataSelected = false;
		}

		// Clear all data collections
		PropertyItems.Clear();
		NameMapItems.Clear();
		ImportItems.Clear();
		ExportItems.Clear();
		CustomVersionItems.Clear();
		SetRowOperationPermissions(false, false); // Not allowed to add or delete

		// Determine TreeNodeType based on node name, consistent with original project
		switch (value.Type)
		{
			case TreeNodeType.GeneralInformation:
				LoadGeneralInformation();
				break;
			case TreeNodeType.NameMap:
				LoadNameMap();
				return;
			case TreeNodeType.Imports:
				LoadImportData();
				return;
			case TreeNodeType.ExportInformation:
				LoadExportInformation();
				return;
			case TreeNodeType.SoftObjectPathList:
				LoadSoftObjectPaths();
				break;
			case TreeNodeType.DependsMap:
				LoadDependsMap();
				break;
			case TreeNodeType.SoftPackageReferences:
				LoadSoftPackageReferences();
				break;
			case TreeNodeType.WorldTileInfo:
				LoadWorldTileInfo();
				break;
			case TreeNodeType.DataResources:
				LoadDataResources();
				break;
			case TreeNodeType.CustomVersionContainer:
				LoadCustomVersionContainer();
				return;
			case TreeNodeType.ExportData:
				LoadExportData();
				break;

			default:
				// Handle individual export nodes and sub-nodes
				if (value is ExportPointingTreeNodeItem exportPointingNode)
				{
					LoadIndividualExport(exportPointingNode);
				}
				else if (value is PointingTreeNodeItem pointingNode && pointingNode.Data != null)
				{
					LoadDataFromPointingNode(pointingNode);
				}
				else if (value.Data != null) LoadDataFromNode(value);
				else
				{
					// Default node information
					PropertyItems.Add(new PropertyItem("Node Name", ""));
					PropertyItems.Add(new PropertyItem("Node Type", ""));
				}
				break;
		}

		// Set current data context to PropertyItems
		CurrentDataContext = PropertyItems;
	}

	/// <summary>
	/// Set row operation permissions
	/// </summary>
	/// <param name="canAdd">Whether to allow adding rows</param>
	/// <param name="canDelete">Whether to allow deleting rows</param>
	private void SetRowOperationPermissions(bool canAdd, bool canDelete)
	{
		CanAddRows = canAdd;
		CanDeleteRows = canDelete;
	}

	private void LoadGeneralInformation()
	{
		if (CurrentAsset == null) return;

		PropertyItems.Clear();
		PropertyItems.Add(new PropertyItem("LegacyFileVersion", CurrentAsset.LegacyFileVersion));
		PropertyItems.Add(new PropertyItem("IsUnversioned", CurrentAsset.IsUnversioned));
		PropertyItems.Add(new PropertyItem("FileVersionLicenseeUE", CurrentAsset.FileVersionLicenseeUE));
		PropertyItems.Add(new PropertyItem("PackageGuid", CurrentAsset.PackageGuid));
		PropertyItems.Add(new PropertyItem("PackageFlags", CurrentAsset.PackageFlags));
		PropertyItems.Add(new PropertyItem("PackageSource", CurrentAsset.PackageSource));

		string packageNameField = CurrentAsset.ObjectVersionUE5 >= ObjectVersionUE5.ADD_SOFTOBJECTPATH_LIST ? "PackageName" : "FolderName";
		PropertyItems.Add(new PropertyItem(packageNameField, CurrentAsset.FolderName));
		PropertyItems.Add(new PropertyItem("EngineVersion", CurrentAsset.GetEngineVersion()));
		PropertyItems.Add(new PropertyItem("ObjectVersion", CurrentAsset.ObjectVersion));
		PropertyItems.Add(new PropertyItem("ObjectVersionUE5", CurrentAsset.ObjectVersionUE5));
	}

	private void LoadNameMap()
	{
		if (CurrentAsset == null) return;

		// Strictly implement according to the original UAssetGUI TableHandler.Load() method
		var nameMapList = CurrentAsset.GetNameMapIndexList();

		// Clear existing data
		NameMapItems.Clear();

		// Check if Case Preserving column is needed
		bool hasCasePreservingColumn = CurrentAsset.GetCustomVersion<UAssetAPI.CustomVersions.FReleaseObjectVersion>() < UAssetAPI.CustomVersions.FReleaseObjectVersion.PropertiesSerializeRepCondition;

		for (int i = 0; i < nameMapList.Count; i++)
		{
			// Create NameMapItem that directly references the asset
			var nameMapItem = new NameMapItem(CurrentAsset, i);
			NameMapItems.Add(nameMapItem);
		}

		// Set current data context to NameMapItems
		CurrentDataContext = NameMapItems;
		SetRowOperationPermissions(false, true); // Not allowed to add, allowed to delete
	}

	private void LoadImportData()
	{
		if (CurrentAsset == null) return;

		// Clear existing data
		ImportItems.Clear();

		for (int i = 0; i < CurrentAsset.Imports.Count; i++)
		{
			var import = CurrentAsset.Imports[i];
			var importItem = new ImportItem(CurrentAsset, import) { Index = -(i + 1) };

			ImportItems.Add(importItem);
		}

		// Set current data context to ImportItems
		CurrentDataContext = ImportItems;
		SetRowOperationPermissions(true, true); // Allowed to add and delete
	}

	private void LoadExportInformation()
	{
		if (CurrentAsset == null) return;

		// Clear ExportItems collection
		ExportItems.Clear();

		for (int i = 0; i < CurrentAsset.Exports.Count; i++)
		{
			var export = CurrentAsset.Exports[i];
			var exportItem = new ExportItem(CurrentAsset, export) { Index = (i + 1) };

			ExportItems.Add(exportItem);
		}

		// Set current data context to ExportItems
		CurrentDataContext = ExportItems;
		SetRowOperationPermissions(true, true); // Allowed to add and delete
	}

	private void LoadSoftObjectPaths()
	{
		if (CurrentAsset != null && CurrentAsset.SoftObjectPathList != null)
		{
			for (int i = 0; i < CurrentAsset.SoftObjectPathList.Count; i++)
			{
				var path = CurrentAsset.SoftObjectPathList[i];
				PropertyItems.Add(new PropertyItem($"SoftObjectPath[{i}]", path));
			}
		}
	}

	private void LoadDependsMap()
	{
		if (CurrentAsset != null && CurrentAsset.DependsMap != null)
		{
			for (int i = 0; i < CurrentAsset.DependsMap.Count; i++)
			{
				for (int j = 0; j < CurrentAsset.DependsMap[i].Length; j++)
				{
					PropertyItems.Add(new PropertyItem($"Depends[{i}][{j}]", CurrentAsset.DependsMap[i][j]));
				}
			}
		}
	}

	private void LoadSoftPackageReferences()
	{
		if (CurrentAsset != null && CurrentAsset.SoftPackageReferenceList != null)
		{
			for (int i = 0; i < CurrentAsset.SoftPackageReferenceList.Count; i++)
			{
				PropertyItems.Add(new PropertyItem($"SoftPackageRef[{i}]", CurrentAsset.SoftPackageReferenceList[i]));
			}
		}
	}

	private void LoadWorldTileInfo()
	{
		if (CurrentAsset != null && CurrentAsset.WorldTileInfo != null)
		{
			PropertyItems.Add(new PropertyItem("Layer", CurrentAsset.WorldTileInfo.Layer));
			PropertyItems.Add(new PropertyItem("LODList.Length", CurrentAsset.WorldTileInfo.LODList.Length));

			for (int i = 0; i < CurrentAsset.WorldTileInfo.LODList.Length; i++)
			{
				PropertyItems.Add(new PropertyItem($"LODList[{i}]", CurrentAsset.WorldTileInfo.LODList[i]));
			}
		}
	}

	private void LoadDataResources()
	{
		if (CurrentAsset == null) return;

		// Data Resources implementation would go here
		PropertyItems.Add(new PropertyItem("Data Resources", "Data Resources not yet implemented", "Info"));
	}

	private void LoadCustomVersionContainer()
	{
		if (CurrentAsset == null) return;

		// Strictly implement according to the original UAssetGUI TableHandler.Load() method
		CustomVersionItems.Clear();

		if (CurrentAsset.CustomVersionContainer != null)
		{
			for (int i = 0; i < CurrentAsset.CustomVersionContainer.Count; i++)
			{
				var customVersion = CurrentAsset.CustomVersionContainer[i];
				var customVersionItem = new CustomVersionItem
				{
					Name = customVersion.FriendlyName ?? customVersion.Key.ToString(),
					Version = customVersion.Version
				};

				CustomVersionItems.Add(customVersionItem);
			}
		}

		CurrentDataContext = CustomVersionItems;
	}

	private void LoadExportData()
	{
		if (CurrentAsset == null) return;

		PropertyItems.Add(new PropertyItem("Export Count", CurrentAsset.Exports.Count));
		PropertyItems.Add(new PropertyItem("Info", "Select individual exports to view details"));
	}

	private void LoadIndividualExport(ExportPointingTreeNodeItem exportPointingNode)
	{
		if (CurrentAsset == null || exportPointingNode.Data is not Export export) return;

		PropertyItems.Add(new PropertyItem("Export Index", CurrentAsset.Exports.IndexOf(export)));
		PropertyItems.Add(new PropertyItem("Object Name", export.ObjectName));
		PropertyItems.Add(new PropertyItem("Class Name", export.GetExportClassType()));
		PropertyItems.Add(new PropertyItem("Outer Index", export.OuterIndex));
		PropertyItems.Add(new PropertyItem("Object Flags", export.ObjectFlags));

		// Add more export-specific properties based on export type
		if (export is NormalExport normalExport)
		{
			PropertyItems.Add(new PropertyItem("Property Count", normalExport.Data.Count));
		}
	}

	private void LoadDataFromPointingNode(PointingTreeNodeItem pointingNode)
	{
		if (CurrentAsset == null || pointingNode.Data == null) return;

		// Child node generation is now handled by ExportPointingTreeNodeItem.BuildChildrenIfNeeded()

		if (pointingNode.Type == TreeNodeType.ByteArray || pointingNode.Type == TreeNodeType.KismetByteArray)
		{
			byte[] data = [];
			if (pointingNode.Type == TreeNodeType.KismetByteArray)
			{
				data = ((StructExport)pointingNode.Data).ScriptBytecodeRaw;
			}
			else if (pointingNode.Data is RawExport rawExport)
			{
				data = rawExport.Data;
			}
			else if (pointingNode.Data is NormalExport normalExport)
			{
				data = normalExport.Extras ?? [];
			}

			string hexData = BitConverter.ToString(data).Replace("-", " ");
			PropertyItems.Add(new PropertyItem("Raw Data", string.IsNullOrWhiteSpace(hexData) ? "zero" : hexData, "ByteArray"));
		}
		else if (pointingNode.Type == TreeNodeType.Kismet)
		{
			var bytecode = ((StructExport)pointingNode.Data).ScriptBytecode;
			PropertyItems.Add(new PropertyItem("Script Bytecode", $"({bytecode.Length} instructions)", "Kismet"));

			// Add pretty bytecode if enabled
			if (UAGConfig.Data.EnablePrettyBytecode)
			{
				try
				{
					KismetSerializer.asset = CurrentAsset;
					var prettyScript = KismetSerializer.SerializeScript(bytecode);
					var scriptJson = new JObject(new JProperty("Script", prettyScript));
					PropertyItems.Add(new PropertyItem("Pretty Bytecode", scriptJson.ToString(Formatting.Indented), "PrettyKismet"));
				}
				catch (Exception ex)
				{
					PropertyItems.Add(new PropertyItem("Pretty Bytecode", $"Error: {ex.Message}", "PrettyKismet"));
				}
			}
		}
		else
		{
			switch (pointingNode.Data)
			{
				case NormalExport normalExport:
					switch (pointingNode.Type)
					{
						case TreeNodeType.Normal:
							AddRowsForPropertyArray(normalExport.Data.ToArray());
							break;
						case TreeNodeType.UserDefinedStructData:
							var userDefinedStructExport = (UserDefinedStructExport)normalExport;
							AddRowsForPropertyArray(userDefinedStructExport.StructData.ToArray());
							break;
						case TreeNodeType.StructData:
							LoadStructExportData((StructExport)normalExport);
							break;
					}
					break;
				case PropertyData property:
					// PropertyData now uses PropertyEditor for handling, no longer displayed in DataGrid
					PropertyItems.Add(new PropertyItem("Property Type", property.PropertyType?.ToString() ?? "Unknown", "String"));
					PropertyItems.Add(new PropertyItem("Property Name", property.Name?.ToString() ?? "Unknown", "String"));
					break;
				case PropertyData[] propertyArray:
					AddRowsForPropertyArray(propertyArray);
					break;
				case PointingDictionaryEntry dictEntry:
					LoadDictionaryEntryData(dictEntry);
					break;
				default:
					PropertyItems.Add(new PropertyItem("Data Type", pointingNode.Data.GetType().Name));
					PropertyItems.Add(new PropertyItem("Data Value", pointingNode.Data.ToString() ?? "null", "String"));
					break;
			}
		}
	}

	private void LoadDictionaryEntryData(PointingDictionaryEntry dictEntry)
	{
		PropertyItems.Add(new PropertyItem("Entry Type", "Dictionary Entry"));
		PropertyItems.Add(new PropertyItem("Key Type", dictEntry.Entry.Key.PropertyType?.Value ?? "Unknown"));
		PropertyItems.Add(new PropertyItem("Value Type", dictEntry.Entry.Value.PropertyType?.Value ?? "Unknown"));
	}

	private void AddRowsForPropertyArray(PropertyData[] propertyArray)
	{
		PropertyItems.Add(new PropertyItem("Property Count", propertyArray.Length));
		PropertyItems.Add(new PropertyItem("Property Types", string.Join(", ", propertyArray.Select(p => p.PropertyType?.ToString() ?? "Unknown").Distinct())));
		CurrentDataContext = PropertyItems;
	}

	private void LoadStructExportData(StructExport structExport)
	{
		PropertyItems.Add(new PropertyItem("Next", structExport.Field.Next));
		PropertyItems.Add(new PropertyItem("Super Struct", structExport.SuperStruct));
		PropertyItems.Add(new PropertyItem("ScriptBytecodeSize", structExport.ScriptBytecodeSize));
		PropertyItems.Add(new PropertyItem("ScriptBytecode", "Raw bytecode data", "ByteArray"));

		PropertyItems.Add(new PropertyItem("---", "CHILDREN", "Header"));
		for (int i = 0; i < structExport.Children.Length; i++)
		{
			PropertyItems.Add(new PropertyItem($"Child[{i}]", structExport.Children[i]));
		}

		PropertyItems.Add(new PropertyItem("---", "LOADED PROPERTIES", "Header"));
		for (int i = 0; i < structExport.LoadedProperties.Length; i++)
		{
			PropertyItems.Add(new PropertyItem(structExport.LoadedProperties[i].Name?.ToString() ?? "Unknown", structExport.LoadedProperties[i].SerializedType?.ToString() ?? "Unknown", "Property"));
		}
	}

	private void LoadDataFromNode(TreeNodeItem selectedNode)
	{
		if (CurrentAsset == null) return;

		// Display basic information of Node
		PropertyItems.Add(new PropertyItem("Node Name", selectedNode.ToString()));
		PropertyItems.Add(new PropertyItem("Node Type", selectedNode.Type.ToString()));
		PropertyItems.Add(new PropertyItem("Data Type", selectedNode.Data?.GetType().Name ?? "null"));
	}

	#endregion

	#region File Operations

	[RelayCommand]
	public void SaveFile()
	{
		if (IsAssetFromBuildPak && AssetUpdateRequested != null)
		{
			SaveCurrentFile(CurrentFilePath, true);
			return;
		}

		if (string.IsNullOrEmpty(CurrentFilePath) || !Path.IsPathRooted(CurrentFilePath))
		{
			SaveFileAs();
			return;
		}

		SaveCurrentFile(CurrentFilePath);
	}

	[RelayCommand]
	void SaveFileAs()
	{
		var saveFileDialog = new SaveFileDialog
		{
			Title = StringHelper.Get("Dialog.SaveAssetFile.Title"),
			FileName = Path.GetFileName(CurrentFilePath),
			Filter = "UAsset Files|*.uasset|JSON Files|*.json|All Files (*.*)|*.*",
			DefaultExt = "uasset"
		};

		if (saveFileDialog.ShowDialog() == true)
		{
			SaveCurrentFile(saveFileDialog.FileName);
		}
	}

	/// <summary>
	/// Save current file
	/// </summary>
	private void SaveCurrentFile(string filePath, bool buildPak = false)
	{
		try
		{
			if (CurrentAsset == null) return;

			var fileExtension = Path.GetExtension(filePath).ToLower();
			switch (fileExtension)
			{
				case ".json":
					File.WriteAllText(filePath, CurrentAsset.SerializeJson());
					break;
				default:
					SaveAsUAsset(filePath, buildPak);
					break;
			}

			CurrentFilePath = filePath;
			HasUnsavedChanges = false;
			UpdateDiscordRpc();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Error saving file: {ex.Message}");
			MessageBox.Show(StringHelper.Get("MainWindow_SaveFileFailedMessage"), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	/// <summary>
	/// Save as UAsset binary format
	/// </summary>
	private void SaveAsUAsset(string filePath, bool buildPak)
	{
		if (CurrentAsset == null) return;

		// Check if asset is from build PAK
		if (buildPak)
		{
			// Save to build PAK instead of disk
			AssetUpdateRequested?.Invoke(this, new AssetUpdateEventArgs(CurrentFilePath, CurrentAsset));
			HasUnsavedChanges = false;
			Status = $"Saved to build Pak: {Path.GetFileName(CurrentFilePath)}";
		}
		else
		{
			// Save as UAsset binary format to disk
			var dir = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

			CurrentAsset.Write(filePath);
			Status = $"Saved to uasset: {Path.GetFileName(CurrentFilePath)}";
		}
	}

	#endregion

	#region Row Operations

	/// <summary>
	/// Add new Export item
	/// </summary>
	public ExportItem AddExport(bool append = false)
	{
		if (CurrentAsset == null) throw new InvalidOperationException("No asset loaded");

		// Create new Export object
		HasUnsavedChanges = true;
		UpdateDiscordRpc();
		var newExport = new Export()
		{
			ObjectName = new FName(CurrentAsset, (string?)null),
			OuterIndex = new FPackageIndex(),
			ClassIndex = new FPackageIndex(),
			SuperIndex = new FPackageIndex(),
			TemplateIndex = new FPackageIndex(),
		};

		// Add to UAsset
		CurrentAsset.Exports.Add(newExport);

		// Create ExportItem
		var item = new ExportItem(CurrentAsset, newExport) { Index = (ExportItems.Count + 1) };
		if (append) ExportItems.Add(item);
		return item;
	}

	/// <summary>
	/// Add new Import item
	/// </summary>
	public ImportItem AddImport(bool append = false)
	{
		if (CurrentAsset == null) throw new InvalidOperationException("No asset loaded");

		// Create new Import object
		HasUnsavedChanges = true;
		UpdateDiscordRpc();
		var newImport = new Import("None", "None", new FPackageIndex(), "None", false, CurrentAsset);

		// Add to UAsset
		CurrentAsset.Imports.Add(newImport);

		// Create ImportItem
		var item = new ImportItem(CurrentAsset, newImport) { Index = -(ImportItems.Count + 1) };
		if (append) ImportItems.Add(item);
		return item;
	}

	/// <summary>
	/// Delete selected items
	/// </summary>
	public int DeleteItems(IList items)
	{
		if (CurrentAsset == null) return 0;
		int deleted = 0;

		// Delete ImportItem
		if (CurrentDataContext is ObservableCollection<ImportItem> importItems)
		{
			foreach (var obj in items)
			{
				if (obj is ImportItem importItem)
				{
					int idx = CurrentAsset.Imports.IndexOf(importItem.Model);
					if (idx >= 0 && idx < CurrentAsset.Imports.Count)
					{
						CurrentAsset.Imports.RemoveAt(idx);
						deleted++;
					}
				}
			}
			LoadImportData();
		}

		// Delete ExportItem
		if (CurrentDataContext is ObservableCollection<ExportItem> exportItems)
		{
			foreach (var obj in items)
			{
				if (obj is ExportItem exportItem)
				{
					int idx = CurrentAsset.Exports.IndexOf(exportItem.Model);
					if (idx >= 0 && idx < CurrentAsset.Exports.Count)
					{
						CurrentAsset.Exports.RemoveAt(idx);
						deleted++;
					}
				}
			}
			// Refresh the ExportInformation list view
			LoadExportInformation();
		}

		if (deleted > 0)
		{
			HasUnsavedChanges = true;
			UpdateDiscordRpc();
		}
		return deleted;
	}

	[RelayCommand]
	void ImportBinaryData()
	{
		if (CurrentAsset == null || SelectedTreeNode is not PointingTreeNodeItem node) return;
		if (node.Type != TreeNodeType.ByteArray && node.Type != TreeNodeType.KismetByteArray) return;

		var dlg = new OpenFileDialog
		{
			Title = "Import Binary Data",
			Filter = "All Files (*.*)|*.*",
			Multiselect = false
		};
		if (dlg.ShowDialog() == true)
		{
			try
			{
				byte[] data = File.ReadAllBytes(dlg.FileName);
				if (!TrySetSelectedNodeBytes(node, data)) return;
				UpdateBinaryNodeTitle(node, data.Length);
				RefreshBinaryPropertyGrid(data);
				HasUnsavedChanges = true;
				Status = StringHelper.Get("MainWindow_Status.FileModified");
				UpdateDiscordRpc();
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_GenericErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	[RelayCommand]
	void ExportBinaryData()
	{
		if (CurrentAsset == null || SelectedTreeNode is not PointingTreeNodeItem node) return;
		if (!TryGetSelectedNodeBytes(node, out var data)) return;

		var dlg = new SaveFileDialog
		{
			Title = "Export Binary Data",
			Filter = "All Files (*.*)|*.*"
		};
		if (dlg.ShowDialog() == true)
		{
			try
			{
				File.WriteAllBytes(dlg.FileName, data);
				Status = $"Exported {data.Length} B";
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_GenericErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	[RelayCommand]
	void SetBinaryData()
	{
		if (CurrentAsset == null || SelectedTreeNode is not PointingTreeNodeItem node) return;
		if (node.Type != TreeNodeType.ByteArray && node.Type != TreeNodeType.KismetByteArray) return;

		var prompt = new TextPromptWindow
		{
			Title = "Set Binary Data",
			DisplayText = "输入十六进制(允许空格/短横线)或以 base64: 前缀的BASE64"
		};
		if (prompt.ShowDialog() == true)
		{
			try
			{
				byte[] data = prompt.OutputText.HexToBytes();
				if (!TrySetSelectedNodeBytes(node, data)) return;
				UpdateBinaryNodeTitle(node, data.Length);
				RefreshBinaryPropertyGrid(data);
				HasUnsavedChanges = true;
				Status = StringHelper.Get("MainWindow_Status.FileModified");
				UpdateDiscordRpc();
			}
			catch (Exception ex)
			{
				MessageBox.Show(StringHelper.Get("MainWindow_GenericErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	private static void UpdateBinaryNodeTitle(PointingTreeNodeItem node, int length)
	{
		if (node.Type == TreeNodeType.KismetByteArray) node.Name = $"ScriptBytecode ({length} B)";
		else
		{
			if (node.Data is RawExport) node.Name = $"Raw Data ({length} B)";
			else node.Name = $"Extras ({length} B)";
		}
	}

	private void RefreshBinaryPropertyGrid(byte[] data)
	{
		PropertyItems.Clear();
		string hexData = UnsafePrint.BytesToHex(data);
		PropertyItems.Add(new PropertyItem("Raw Data", string.IsNullOrWhiteSpace(hexData) ? "zero" : hexData, "ByteArray"));
		CurrentDataContext = PropertyItems;
	}

	private static bool TryGetSelectedNodeBytes(PointingTreeNodeItem node, out byte[] data)
	{
		data = Array.Empty<byte>();
		if (node.Type == TreeNodeType.KismetByteArray && node.Data is StructExport se)
		{
			data = se.ScriptBytecodeRaw ?? Array.Empty<byte>();
			return true;
		}
		if (node.Type == TreeNodeType.ByteArray)
		{
			if (node.Data is RawExport re)
			{
				data = re.Data ?? Array.Empty<byte>();
				return true;
			}
			if (node.Data is NormalExport ne)
			{
				data = ne.Extras ?? Array.Empty<byte>();
				return true;
			}
		}
		return false;
	}

	private static bool TrySetSelectedNodeBytes(PointingTreeNodeItem node, byte[] data)
	{
		if (node.Type == TreeNodeType.KismetByteArray && node.Data is StructExport se)
		{
			se.ScriptBytecodeRaw = data ?? Array.Empty<byte>();
			return true;
		}
		if (node.Type == TreeNodeType.ByteArray)
		{
			if (node.Data is RawExport re)
			{
				re.Data = data ?? Array.Empty<byte>();
				return true;
			}
			if (node.Data is NormalExport ne)
			{
				ne.Extras = data ?? Array.Empty<byte>();
				return true;
			}
		}
		return false;
	}

	#endregion

	#region Search Provider

	bool ITreeSearchProvider.FindNext(Func<object, bool> predicate, bool isForward, CancellationToken token, out object? selected)
	{
		selected = null;
		if (predicate == null) return false;
		var node = SelectedTreeNode;
		var forward = isForward;
		var visited = new HashSet<TreeNodeItem>();
		while (node != null)
		{
			if (token.IsCancellationRequested) return false;
			if (!visited.Add(node)) break;
			if (node != SelectedTreeNode && predicate(node)) { selected = node; return true; }
			if (NodeDeepMatches(node, predicate)) { selected = node; return true; }
			node = GetNextNode(node, forward);
		}
		return false;
	}

	List<object> ITreeSearchProvider.FindAll(Func<object, bool> predicate, CancellationToken token)
	{
		var results = new List<object>();
		if (predicate == null) return results;
		foreach (var root in TreeNodes)
		{
			if (token.IsCancellationRequested) break;
			Traverse(root, n =>
			{
				if (token.IsCancellationRequested) return;
				n.IsSearchMatched = predicate(n);
				if (n.IsSearchMatched) results.Add(n);
				foreach (var _ in EnumerateNodeDeepMatches(n, predicate))
				{
					results.Add(n);
					break;
				}
			});
		}
		return results.Distinct().Cast<object>().ToList();
	}

	void ITreeSearchProvider.ClearHighlights()
	{
		foreach (var root in TreeNodes) Traverse(root, n => n.IsSearchMatched = false);
	}

	void ITreeSearchProvider.SelectNode(TreeNodeItem node)
	{
		SelectedTreeNode = node;
	}

	private static TreeNodeItem? GetNextNode(TreeNodeItem node, bool forward, bool canGoDown = true)
	{
		if (forward)
		{
			if (node.Children.Count != 0 && canGoDown) return node.Children[0];
			var parent = node.Parent;
			if (parent == null) return null;
			int idx = parent.Children.IndexOf(node);
			if (idx >= 0 && idx + 1 < parent.Children.Count) return parent.Children[idx + 1];
			return GetNextNode(parent, forward, false);
		}
		else
		{
			var parent = node.Parent;
			if (parent == null) return null;
			int idx = parent.Children.IndexOf(node);
			if (idx > 0) return GetLastDescendant(parent.Children[idx - 1]);
			return parent;
		}
	}

	private static TreeNodeItem GetLastDescendant(TreeNodeItem node)
	{
		if (node.Children.Count == 0) return node;
		return GetLastDescendant(node.Children[node.Children.Count - 1]);
	}

	private static IEnumerable<object> EnumerateNodeDeepMatches(TreeNodeItem node, Func<object, bool> predicate)
	{
		object? pointer = node.Data;
		if (pointer == null) yield break;
		if (predicate(node)) { yield return node; yield break; }
	}

	private static IEnumerable<(string text, string context, string display)> EnumerateObjectStrings(object obj, HashSet<object> visited, int depth, int maxDepth)
	{
		if (obj == null || depth > maxDepth) yield break;
		if (!obj.GetType().IsValueType)
		{
			if (visited.Contains(obj)) yield break;
			visited.Add(obj);
		}
		if (obj is string s) { yield return (s, "string", s); yield break; }
		if (obj is System.Collections.IEnumerable en && obj is not string)
		{
			int idx = 0; int budget = 2048;
			foreach (var item in en)
			{
				if (--budget < 0) break;
				foreach (var r in EnumerateObjectStrings(item!, visited, depth + 1, maxDepth))
					yield return ($"{r.text}", $"[{idx}] {r.context}", r.display);
				idx++;
			}
			yield break;
		}
		var toStringVal = obj.ToString() ?? string.Empty;
		if (!string.IsNullOrWhiteSpace(toStringVal)) yield return (toStringVal, obj.GetType().Name, toStringVal);
		foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.GetIndexParameters().Length > 0) continue;
			object? val = null; try { val = prop.GetValue(obj); } catch { continue; }
			if (val == null) continue;
			if (val is string vs) { yield return (vs, prop.Name, $"{prop.Name}: {vs}"); continue; }
			foreach (var r in EnumerateObjectStrings(val, visited, depth + 1, maxDepth))
				yield return (r.text, $"{prop.Name}.{r.context}", r.display);
		}
	}

	private static void Traverse(TreeNodeItem node, Action<TreeNodeItem> cb)
	{
		cb(node);
		foreach (var c in node.Children) Traverse(c, cb);
	}

	private static bool NodeDeepMatches(TreeNodeItem node, Func<object, bool> predicate)
	{
		foreach (var _ in EnumerateNodeDeepMatches(node, predicate)) return true;
		return false;
	}

	#endregion

	#region Plugin Management

	[RelayCommand]
	void ExecutePlugin(IUAssetPlugin plugin)
	{
		try
		{
			_pluginLoader?.ExecutePlugin(plugin);
		}
		catch (Exception ex)
		{
			ShowError(StringHelper.Get("Plugin_ExecutionFailed", ex.Message));
		}
	}

	[RelayCommand]
	void OpenPluginsFolder()
	{
		if (_pluginLoader == null) return;
		var pluginsPath = _pluginLoader.GetPluginDirectory();
		Directory.CreateDirectory(pluginsPath);
		Process.Start("explorer.exe", pluginsPath);
	}

	[RelayCommand]
	void ReloadPlugins()
	{
		try
		{
			_pluginLoader?.ReloadPlugins();
			OnPropertyChanged(nameof(Plugins));
			ShowMessage(StringHelper.Get("Plugin_ReloadedSuccessfully"));
		}
		catch (Exception ex)
		{
			ShowError(StringHelper.Get("Plugin_ReloadFailed", ex.Message));
		}
	}


	private void InitializePluginSystem()
	{
		try
		{
			var pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
			_pluginLoader = new PluginLoader(pluginsPath);
			_pluginLoader.Initialize(this);
			OnPropertyChanged(nameof(Plugins));
		}
		catch (Exception ex)
		{
			Debug.WriteLine(StringHelper.Get("Plugin_InitlizeFailed", ex.Message));
		}
	}

	public void CleanupPluginSystem()
	{
		_pluginLoader?.CleanupPlugins();
	}

	public UAsset? GetCurrentAsset() => CurrentAsset;

	public string GetConfigDirectory()
	{
		var configDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"UAssetManager");
		Directory.CreateDirectory(configDir);
		return configDir;
	}

	public UAsset? LoadAsset(string path)
	{
		try
		{
			if (!File.Exists(path))
				throw new FileNotFoundException($"Asset file not found: {path}");

			return new UAsset(path, SelectedEngineVersion);
		}
		catch (Exception ex)
		{
			ShowError($"Failed to load asset: {ex.Message}", "Load Error");
			return null;
		}
	}

	public void SaveAsset(UAsset asset, string path)
	{
		try
		{
			asset.Write(path);
		}
		catch (Exception ex)
		{
			ShowError($"Failed to save asset: {ex.Message}", "Save Error");
		}
	}

	public void ShowMessage(string message, string title = "Plugin Message")
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
		});
	}

	public void ShowError(string message, string title = "Plugin Error")
	{
		Application.Current.Dispatcher.Invoke(() =>
		{
			MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
		});
	}

	public IProgressReporter ShowProgress(string title, int total)
	{
		return new ProgressReporter(title, total);
	}

	#endregion

	#region Discord RPC

	private DiscordRpcClient? _discordRpc;
	private RichPresence? _richPresence;
	private DateTime _lastOpenedTime;

	private DiscordRpcClient DiscordRpc
	{
		get
		{
			if (_discordRpc == null || _discordRpc.IsDisposed)
			{
				_discordRpc = new DiscordRpcClient("1035701531342811156");
				_discordRpc.Initialize();
			}
			else if (!_discordRpc.IsInitialized)
			{
				_discordRpc.Initialize();
			}
			return _discordRpc;
		}
	}

	public void UpdateDiscordRpc()
	{
		if (!UAGConfig.Data.EnableDiscordRPC)
		{
			ClearDiscordRpc();
			return;
		}
		if (DiscordRpc == null || !DiscordRpc.IsInitialized || DiscordRpc.IsDisposed) return;

		try
		{
			_richPresence ??= new RichPresence
			{
				Timestamps = new Timestamps(),
				Assets = new Assets() { LargeImageKey = "main_logo" }
			};

			bool isEditingAsset = !string.IsNullOrEmpty(CurrentFilePath) && HasUnsavedChanges;
			string projectName = GetProjectName();

			_richPresence.Details = projectName != null ? $"Project: {projectName} ({UAGConfig.Data.PreferredVersion})" : string.Empty;
			_richPresence.State = isEditingAsset ? $"File: {Path.GetFileName(CurrentFilePath)}" : "Idling";
			_richPresence.Timestamps.Start = _lastOpenedTime;
			DiscordRpc.SetPresence(_richPresence);
		}
		catch (Exception ex)
		{
			// Silently fail - Discord RPC is not critical
			Debug.WriteLine($"Discord RPC Error: {ex.Message}");
		}
	}

	public void ClearDiscordRpc()
	{
		try
		{
			if (DiscordRpc != null && DiscordRpc.IsInitialized && !DiscordRpc.IsDisposed)
			{
				DiscordRpc.ClearPresence();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Discord RPC Clear Error: {ex.Message}");
		}
	}

	public void DisposeDiscordRpc()
	{
		try
		{
			if (_discordRpc != null && !_discordRpc.IsDisposed)
			{
				_discordRpc.ClearPresence();
				_discordRpc.Dispose();
				_discordRpc = null;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Discord RPC Dispose Error: {ex.Message}");
		}
	}

	private string? GetProjectName()
	{
		if (string.IsNullOrEmpty(CurrentFilePath)) return null;

		try
		{
			var directory = Path.GetDirectoryName(CurrentFilePath);
			if (string.IsNullOrEmpty(directory)) return null;

			// Look for common project indicators
			var projectFiles = Directory.GetFiles(directory, "*.uproject", SearchOption.TopDirectoryOnly);
			if (projectFiles.Length > 0)
			{
				return Path.GetFileNameWithoutExtension(projectFiles[0]);
			}

			// Fallback to parent directory name
			return Path.GetFileName(directory);
		}
		catch
		{
			return null;
		}
	}

	#endregion
}