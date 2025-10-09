using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using UAssetAPI;
using UAssetManager.Models;
using UAssetManager.Pak;
using UAssetManager.Pak.Compression;
using UAssetManager.Pak.Encryption.Aes;
using UAssetManager.Pak.Objects;
using UAssetManager.Pak.Pak.Objects;
using UAssetManager.Resources;
using UAssetManager.Utils;
using UAssetManager.Views;

namespace UAssetManager.ViewModels;
internal partial class FileContainerViewModel : ObservableObject
{
    #region Fields
    [ObservableProperty] string _currentContainerPath = string.Empty;
    [ObservableProperty] string _currentPakName = string.Empty;
    [ObservableProperty] string _extractStatus = string.Empty;
    [ObservableProperty] bool _isExtracting = false;
    [ObservableProperty] int _extractTotal = 0;
    [ObservableProperty] int _extractCurrent = 0;

    private PakFileReader _buildPakReader = new();
    private DirectoryTreeItem? _copiedItem;
    private bool _shouldDeleteCopiedItem = false;
    private CancellationTokenSource? _extractCts;

    public ObservableCollection<DirectoryTreeItem> LoadTreeItems { get; } = new();
    public ObservableCollection<DirectoryTreeItem> SaveTreeItems { get; } = new();
    #endregion

    #region Load
    [RelayCommand]
    void Load()
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = StringHelper.Get("FileContainer_OpenPak"),
            Filter = "PAK Files (*.pak)|*.pak|All Files (*.*)|*.*",
            Multiselect = false
        };

        if (openFileDialog.ShowDialog() == true)
        {
            LoadFromPak(openFileDialog.FileName, UAGConfig.Data.AesKey);
        }
    }

    [RelayCommand]
    void CopyPath(DirectoryTreeItem? selectedItem)
    {
        if (selectedItem == null) return;

        string path = GetFullPath(selectedItem);
        Clipboard.SetText(path);
    }

    private string GetFullPath(DirectoryTreeItem item)
    {
        var pathParts = new List<string>();
        var current = item;

        while (current != null)
        {
            pathParts.Insert(0, current.Name);
            current = current.Parent;
        }

        string fullPath = string.Join("/", pathParts);

        // Map Unreal Engine content paths: GameName/Content/ -> /Game/
        return Regex.Replace(fullPath, @"^[^/]+/Content/", "/Game/", RegexOptions.IgnoreCase);
    }

    [RelayCommand]
    async Task ExtractSelected(DirectoryTreeItem? selectedItem)
    {
        if (selectedItem == null) return;

        var dlg = new OpenFolderDialog { Title = StringHelper.Get("FileContainer_ExtractFolderMessage") };
        if (dlg.ShowDialog() == true)
        {
            _extractCts?.Dispose();
            _extractCts = new CancellationTokenSource();

            ExtractTotal = selectedItem.GetFileCount();
            ExtractCurrent = 0;
            IsExtracting = true;
            ExtractStatus = StringHelper.Get("FileContainer_ExtractingMessage", 0, ExtractTotal);

            var progress = new Progress<int>(_ =>
            {
                ExtractCurrent++;
                ExtractStatus = StringHelper.Get("FileContainer_ExtractingMessage", ExtractCurrent, ExtractTotal);
            });

            try
            {
                int extracted = await ExtractItemAsync(selectedItem, dlg.FolderName, progress, _extractCts.Token).ConfigureAwait(false);
                UAGUtils.OpenFolder(dlg.FolderName);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(
                    StringHelper.Get("FileContainer_ExtractCancelledMessage", ExtractCurrent, ExtractTotal),
                    StringHelper.Get("Text.Cancelled"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(StringHelper.Get("FileContainer_ExtractErrorMessage", ex.Message),
                    StringHelper.Get("Text.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ExtractStatus = string.Empty;
                IsExtracting = false;
                ExtractCurrent = 0;
                ExtractTotal = 0;
                _extractCts.Dispose();
                _extractCts = null;
            }
        }
    }

    [RelayCommand]
    void CancelExtract()
    {
        _extractCts?.Cancel();
    }

    [RelayCommand]
    void Save()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = StringHelper.Get("FileContainer_SavePak"),
            Filter = "PAK Files (*.pak)|*.pak|All Files (*.*)|*.*",
            DefaultExt = "pak"
        };
        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                SaveToPak(saveFileDialog.FileName);
                MessageBox.Show(StringHelper.Get("FileContainer_BuildCompletedMessage"), StringHelper.Get("Text.Completed"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(StringHelper.Get("FileContainer_BuildFailedMessage", ex.Message), StringHelper.Get("Text.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    void Refresh()
    {
        RefreshSaveTree();
    }

    [RelayCommand]
    void Cut(DirectoryTreeItem? selectedItem)
    {
        if (selectedItem != null)
        {
            if (!selectedItem.IsFile)
            {
                MessageBox.Show(StringHelper.Get("FileContainer_CutFoldersNotSupportedMessage"), StringHelper.Get("Text.Information"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _copiedItem = selectedItem;
            _shouldDeleteCopiedItem = true;
        }
    }

    [RelayCommand]
    void Copy(DirectoryTreeItem? selectedItem)
    {
        if (selectedItem != null)
        {
            if (!selectedItem.IsFile)
            {
                MessageBox.Show(StringHelper.Get("FileContainer_CopyFoldersNotSupportedMessage"), StringHelper.Get("Text.Information"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _copiedItem = selectedItem;
            _shouldDeleteCopiedItem = false;
        }
    }

    public void LoadFromPak(string path, string? aesKey = null)
    {
        CurrentContainerPath = path;
        CurrentPakName = Path.GetFileName(path) ?? string.Empty;
        LoadTreeItems.Clear();

        var reader = new PakFileReader(path);
        if (!string.IsNullOrWhiteSpace(aesKey))
        {
            reader.AesKey = new FAesKey(aesKey);
        }
        reader.Mount(StringComparer.OrdinalIgnoreCase);

        var directoryTree = new DirectoryTree();
        var processedFiles = new HashSet<string>();

        foreach (var kv in reader.Files)
        {
            if (processedFiles.Contains(kv.Key)) continue;

            // Collect related files (.uasset, .uexp, .ubulk)
            var relatedFiles = CollectRelatedFiles(kv.Key, reader.Files);
            processedFiles.UnionWith(relatedFiles.Select(f => f.Path));

            directoryTree.CreateNode(kv.Key, null, null, null, relatedFiles);
        }

        foreach (var root in directoryTree.RootNodes.Values)
        {
            LoadTreeItems.Add(root);
        }
    }

    private List<GameFile> CollectRelatedFiles(string mainPath, IDictionary<string, GameFile> allFiles)
    {
        var relatedFiles = new List<GameFile>();
        var basePath = Path.GetFileNameWithoutExtension(mainPath);
        var directory = Path.GetDirectoryName(mainPath)?.Replace('\\', '/') ?? "";

        // Add the main file
        if (allFiles.TryGetValue(mainPath, out var mainFile))
        {
            relatedFiles.Add(mainFile);
        }

        // Look for related files (.uexp, .ubulk)
        var extensions = new[] { ".uexp", ".ubulk" };
        foreach (var ext in extensions)
        {
            var relatedPath = string.IsNullOrEmpty(directory)
                ? basePath + ext
                : directory + "/" + basePath + ext;

            if (allFiles.TryGetValue(relatedPath, out var relatedFile))
            {
                relatedFiles.Add(relatedFile);
            }
        }

        return relatedFiles;
    }

    public async Task<int> ExtractItemAsync(DirectoryTreeItem node, string targetDir, IProgress<int>? progress, CancellationToken token)
    {
        if (token.IsCancellationRequested) return 0;
        int count = 0;
        if (node.IsFile)
        {
            try
            {
                if (node.GameFiles.Count > 0)
                {
                    foreach (var gameFile in node.GameFiles)
                    {
                        if (gameFile is FPakEntry entry)
                        {
                            var data = await Task.Run(() => entry.Read(), token).ConfigureAwait(false);
                            if (data != null)
                            {
                                var outPath = Path.Combine(targetDir, gameFile.Path.Replace('/', Path.DirectorySeparatorChar));
                                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                                await File.WriteAllBytesAsync(outPath, data, token).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch { }
            progress?.Report(1);
            count++;
        }
        else
        {
            foreach (var child in node.Children.Values)
            {
                if (token.IsCancellationRequested) break;
                count += await ExtractItemAsync(child, targetDir, progress, token).ConfigureAwait(false);
            }
        }
        return count;
    }

    #endregion

    #region Save

    [RelayCommand]
    void Paste(DirectoryTreeItem? targetItem)
    {
        if (_copiedItem == null || _buildPakReader == null) return;
        if (targetItem != null)
        {
            var targetDirectory = targetItem.IsFile ? targetItem.Parent : targetItem;
            var desiredGamePath = Path.Combine(targetDirectory?.FullPath ?? string.Empty, _copiedItem.Name)
                .Replace(Path.DirectorySeparatorChar, '/');

            // Check if the copied item is from the build pak (right side) or source pak (left side)
            bool isFromBuildPak = _buildPakReader.Files.ContainsKey(_copiedItem.FullPath);
            if (isFromBuildPak)
            {
                // Item is from build pak - handle move/duplicate within build pak
                if (_shouldDeleteCopiedItem)
                {
                    // move - remove old entry and add new one
                    if (_buildPakReader.Files.TryGetValue(_copiedItem.FullPath, out var entryToMove))
                    {
                        _buildPakReader.Files.Remove(_copiedItem.FullPath);
                        _buildPakReader.Files[desiredGamePath] = entryToMove;
                    }
                    _shouldDeleteCopiedItem = false;
                }
                else
                {
                    // duplicate - copy the entry
                    if (_buildPakReader.Files.TryGetValue(_copiedItem.FullPath, out var entryToCopy))
                    {
                        _buildPakReader.Files[desiredGamePath] = entryToCopy;
                    }
                }
            }
            else
            {
                // Item is from source pak (left side) - add to build pak
                if (_copiedItem.GameFiles.Count > 0)
                {
                    foreach (var gameFile in _copiedItem.GameFiles)
                    {
                        // For related files, maintain their relative structure
                        var relativePath = Path.GetRelativePath(_copiedItem.FullPath, gameFile.Path).Replace('\\', '/');
                        var newGamePath = string.IsNullOrEmpty(relativePath) || relativePath == "."
                            ? desiredGamePath
                            : Path.Combine(Path.GetDirectoryName(desiredGamePath) ?? "", relativePath).Replace('\\', '/');

                        // Create a new FPakEntry with the new path
                        var newEntry = new FPakEntry(_buildPakReader, gameFile.Read(), newGamePath);
                        _buildPakReader.Files.Add(newGamePath, newEntry);
                    }
                }
            }

            RefreshSaveTree(true);
            ExpandSaveTreeTo(desiredGamePath);
        }
        _copiedItem = null;
    }

    [RelayCommand]
    void Delete(DirectoryTreeItem? selectedItem)
    {
        if (selectedItem != null && _buildPakReader != null)
        {
            var result = MessageBox.Show(StringHelper.Get("FileContainer_ConfirmDeleteMessage", selectedItem.Name),
                StringHelper.Get("FileContainer_ConfirmDelete"), MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (selectedItem.IsFile)
                {
                    _buildPakReader.Files.Remove(selectedItem.FullPath);
                }
                else
                {
                    string prefix = selectedItem.FullPath.TrimEnd('/') + "/";
                    var keysToRemove = _buildPakReader.Files.Keys
                        .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    foreach (var key in keysToRemove)
                    {
                        _buildPakReader.Files.Remove(key);
                    }
                }
                RefreshSaveTree(true);
            }
        }
    }

    [RelayCommand]
    void NewFolder(DirectoryTreeItem? baseItem)
    {
        if (_buildPakReader == null) return;

        string basePath = string.Empty;
        if (baseItem != null)
        {
            basePath = (baseItem.IsFile ? baseItem.Parent?.FullPath : baseItem.FullPath) ?? string.Empty;
        }

        var textPrompt = new TextPromptWindow
        {
            DisplayText = StringHelper.Get("FileContainer_NewFolderPrompt"),
            PrefilledText = "NewFolder"
        };
        if (textPrompt.ShowDialog() == true)
        {
            var name = textPrompt.OutputText.Trim().Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(name)) return;
            var full = string.IsNullOrEmpty(basePath) ? name : (basePath.TrimEnd('/') + "/" + name);

            // Create a virtual file to ensure the folder is displayed in the tree
            var placeholderPath = full + "/.placeholder";
            var emptyData = new byte[0];
            _buildPakReader.Files.Add(placeholderPath, new FPakEntry(_buildPakReader, emptyData, placeholderPath));
            RefreshSaveTree(true);
        }
    }

    [RelayCommand]
    void Rename(DirectoryTreeItem? item)
    {
        if (item == null || _buildPakReader == null) return;
        var textPrompt = new TextPromptWindow
        {
            DisplayText = StringHelper.Get("FileContainer_RenamePrompt"),
            PrefilledText = item.Name
        };
        if (textPrompt.ShowDialog() == true)
        {
            var newName = textPrompt.OutputText.Trim().Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(newName)) return;
            var parentPath = item.Parent?.FullPath ?? string.Empty;
            var newPath = string.IsNullOrEmpty(parentPath) ? newName : (parentPath.TrimEnd('/') + "/" + newName);

            if (item.IsFile)
            {
                if (_buildPakReader.Files.TryGetValue(item.FullPath, out var entry))
                {
                    _buildPakReader.Files.Remove(item.FullPath);
                    _buildPakReader.Files[newPath] = entry;
                }
            }
            else
            {
                string oldPrefix = item.FullPath.TrimEnd('/') + "/";
                string newPrefix = newPath.TrimEnd('/') + "/";
                var keysToRename = _buildPakReader.Files.Keys
                    .Where(k => k.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var key in keysToRename)
                {
                    if (_buildPakReader.Files.TryGetValue(key, out var entry))
                    {
                        var newKey = newPrefix + key.Substring(oldPrefix.Length);
                        _buildPakReader.Files.Remove(key);
                        _buildPakReader.Files[newKey] = entry;
                    }
                }
            }
            RefreshSaveTree(true);
        }
    }

    [RelayCommand]
    void AddFromPak(DirectoryTreeItem? item)
    {
        if (item == null || _buildPakReader == null) return;
        if (!item.IsFile)
        {
            foreach (var child in item.Children) AddFromPak(child.Value);
        }

        // Check if any of the game files already exist in build pak
        var existingFiles = new List<string>();
        foreach (var gameFile in item.GameFiles)
        {
            if (_buildPakReader.Files.ContainsKey(gameFile.Path))
            {
                existingFiles.Add(gameFile.Path);
            }
        }

        if (existingFiles.Count > 0)
        {
            var message = existingFiles.Count == 1
                ? StringHelper.Get("FileContainer_FileAlreadyInBuildMessage")
                : StringHelper.Get("FileContainer_FilesAlreadyInBuildMessage", existingFiles.Count);
            MessageBox.Show(message, StringHelper.Get("Text.Information"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Add game files directly to build pak
        foreach (var gameFile in item.GameFiles)
        {
            _buildPakReader.AddFile(gameFile);
        }

        RefreshSaveTree(true);
        ExpandSaveTreeTo(item.FullPath);
    }

    [RelayCommand]
    void AddFromDisk(DirectoryTreeItem? item)
    {
        if (_buildPakReader == null) return;

        var openFileDialog = new OpenFileDialog
        {
            Title = StringHelper.Get("FileContainer_AddFileTitle"),
            Filter = "All Files (*.*)|*.*",
            Multiselect = false
        };
        if (openFileDialog.ShowDialog() == true)
        {
            string sourcePath = openFileDialog.FileName;
            string fileName = Path.GetFileName(sourcePath);
            string targetBase = item == null ? string.Empty : ((item.IsFile ? item.Parent?.FullPath : item.FullPath) ?? string.Empty);
            string gamePath = string.IsNullOrEmpty(targetBase) ? fileName : (targetBase.TrimEnd('/') + "/" + fileName);

            _buildPakReader.AddFile(sourcePath, gamePath);
            RefreshSaveTree(true);
            ExpandSaveTreeTo(targetBase);
        }
    }

    [RelayCommand]
    void AddFolderFromDisk(DirectoryTreeItem? baseItem)
    {
        if (_buildPakReader == null) return;

        var dlg = new OpenFolderDialog { Title = StringHelper.Get("FileContainer_ExtractFolderMessage") };
        if (dlg.ShowDialog() == true)
        {
            string selectedBase = dlg.FolderName;
            string targetBase = baseItem == null ? string.Empty : ((baseItem.IsFile ? baseItem.Parent?.FullPath : baseItem.FullPath) ?? string.Empty);

            // Directories will be created automatically when files are added
            foreach (var file in Directory.EnumerateFiles(selectedBase, "*", SearchOption.AllDirectories))
            {
                try
                {
                    string relFile = Path.GetRelativePath(Path.GetDirectoryName(selectedBase)!, file).Replace('\\', '/');
                    string gamePath = string.IsNullOrEmpty(targetBase) ? relFile : (targetBase.TrimEnd('/') + "/" + relFile);
                    _buildPakReader.AddFile(file, gamePath, gamePath.EndsWith(".uasset") && false ? CompressionMethod.None : CompressionMethod.Oodle);
                }
                catch (Exception)
                {

                }
            }

            RefreshSaveTree(true);
            ExpandSaveTreeTo(targetBase);
        }
    }

    public void AddFromDisk(string sourcePath, string? desiredGamePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || _buildPakReader == null) return;
        string gamePath = string.IsNullOrEmpty(desiredGamePath)
            ? Path.GetFileName(sourcePath) ?? string.Empty
            : desiredGamePath;
        gamePath = gamePath.Replace('\\', '/');
        _buildPakReader.AddFile(sourcePath, gamePath);
        RefreshSaveTree();
    }

    private void RefreshSaveTree(bool preserveExpansionState = false)
    {
        if (_buildPakReader == null) return;

        // Save current expanded state
        var expandedPaths = new HashSet<string>();
        if (preserveExpansionState)
        {
            CollectExpandedPaths(SaveTreeItems, expandedPaths);
        }

        SaveTreeItems.Clear();
        var directoryTree = new DirectoryTree();
        var processedFiles = new HashSet<string>();

        foreach (var kvp in _buildPakReader.Files)
        {
            if (processedFiles.Contains(kvp.Key)) continue;

            // Collect related files (.uasset, .uexp, .ubulk)
            var relatedFiles = CollectRelatedFiles(kvp.Key, _buildPakReader.Files);
            processedFiles.UnionWith(relatedFiles.Select(f => f.Path));

            directoryTree.CreateNode(kvp.Key, null, null, null, relatedFiles);
        }
        foreach (var rootNode in directoryTree.RootNodes.Values)
        {
            SaveTreeItems.Add(rootNode);
        }

        // Restore expanded state
        if (preserveExpansionState)
        {
            RestoreExpandedPaths(SaveTreeItems, expandedPaths);
        }
    }

    private void CollectExpandedPaths(IEnumerable<DirectoryTreeItem> items, HashSet<string> expandedPaths)
    {
        foreach (var item in items)
        {
            if (item.IsExpanded)
            {
                expandedPaths.Add(item.FullPath);
            }
            CollectExpandedPaths(item.Children.Values, expandedPaths);
        }
    }

    private void RestoreExpandedPaths(IEnumerable<DirectoryTreeItem> items, HashSet<string> expandedPaths)
    {
        foreach (var item in items)
        {
            if (expandedPaths.Contains(item.FullPath))
            {
                item.IsExpanded = true;
            }
            RestoreExpandedPaths(item.Children.Values, expandedPaths);
        }
    }

    private void ExpandSaveTreeTo(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath)) return;
        var parts = gamePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        var current = SaveTreeItems.FirstOrDefault(r => string.Equals(r.Name, parts[0], StringComparison.OrdinalIgnoreCase));
        if (current == null) return;
        current.IsExpanded = true;
        for (int i = 1; i < parts.Length; i++)
        {
            if (!current.Children.TryGetValue(parts[i], out var next)) break;
            next.IsExpanded = true;
            current = next;
        }
    }

    private void SaveToPak(string outputPakPath)
    {
        if (_buildPakReader == null || _buildPakReader.Files.Count == 0)
            throw new InvalidOperationException(StringHelper.Get("FileContainer_NoFilesToSave"));

        // save pak
        var ms = new MemoryStream();
        _buildPakReader.Write(new UnrealBinaryWriter(ms));
        File.WriteAllBytes(outputPakPath, ms.ToArray());
    }

    public void MoveItem(string oldPath, string newPath)
    {
        if (_buildPakReader?.Files.TryGetValue(oldPath, out var entry) == true)
        {
            _buildPakReader.Files.Remove(oldPath);
            _buildPakReader.Files[newPath] = entry;
            RefreshSaveTree(true);
        }
    }

    public void MoveFolder(string oldFolderPath, string newFolderPath)
    {
        if (_buildPakReader == null) return;

        string oldPrefix = oldFolderPath.TrimEnd('/') + "/";
        string newPrefix = newFolderPath.TrimEnd('/') + "/";

        var keysToMove = _buildPakReader.Files.Keys
            .Where(k => k.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keysToMove)
        {
            if (_buildPakReader.Files.TryGetValue(key, out var entry))
            {
                var newKey = newPrefix + key.Substring(oldPrefix.Length);
                _buildPakReader.Files.Remove(key);
                _buildPakReader.Files[newKey] = entry;
            }
        }

        RefreshSaveTree(true);
    }

    /// <summary>
    /// Updates an asset in the build PAK with new data
    /// </summary>
    /// <param name="gamePath">The game path of the asset to update</param>
    /// <param name="uAsset">The UAsset object to update</param>
    public void UpdateAssetInBuildPak(string gamePath, UAsset uAsset)
    {
        if (_buildPakReader == null) return;

        try
        {
            // Use UAsset.Write method to get both .uasset and .uexp data
            uAsset.Write(out MemoryStream uassetStream, out MemoryStream uexpStream);

            // Create new FPakEntry for .uasset file
            var uassetData = uassetStream.ToArray();
            var newUassetEntry = new FPakEntry(_buildPakReader, uassetData, gamePath);
            _buildPakReader.Files[gamePath] = newUassetEntry;

            // Create new FPakEntry for .uexp file if it exists
            if (uexpStream.Length > 0)
            {
                var uexpPath = Path.ChangeExtension(gamePath, ".uexp");
                var uexpData = uexpStream.ToArray();
                var newUexpEntry = new FPakEntry(_buildPakReader, uexpData, uexpPath);
                _buildPakReader.Files[uexpPath] = newUexpEntry;
            }

            // Check for .ubulk file and update if it exists
            var ubulkPath = Path.ChangeExtension(gamePath, ".ubulk");
            if (_buildPakReader.Files.ContainsKey(ubulkPath))
            {
                // Keep existing .ubulk data for now
                // In a more sophisticated implementation, you might want to update .ubulk data as well
            }

            // Refresh the save tree to reflect changes
            RefreshSaveTree(true);
        }
        catch (Exception ex)
        {
            // Handle error - you might want to show a message to the user
            System.Diagnostics.Debug.WriteLine($"Failed to update asset in build PAK: {ex.Message}");
        }
    }

    #endregion
}

/// <summary>
/// Event arguments for asset update events
/// </summary>
public class AssetUpdateEventArgs : EventArgs
{
    /// <summary>
    /// The game path of the asset to update
    /// </summary>
    public string GamePath { get; }

    /// <summary>
    /// The Asset object to update
    /// </summary>
    public UAsset Asset { get; }

    public AssetUpdateEventArgs(string gamePath, UAsset asset)
    {
        GamePath = gamePath;
        Asset = asset;
    }
}