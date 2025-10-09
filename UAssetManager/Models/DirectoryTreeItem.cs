using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Windows;
using UAssetAPI;
using UAssetManager.Pak.Objects;
using UAssetManager.Views;

namespace UAssetManager.Models;
public partial class DirectoryTreeItem : ObservableObject
{
    [ObservableProperty] List<GameFile> _gameFiles = [];
    [ObservableProperty] string _name = string.Empty;
    [ObservableProperty] string _fullPath = string.Empty;
    [ObservableProperty] string _fixedPathOnDisk = string.Empty;
    [ObservableProperty] bool _isExpanded = false;
    [ObservableProperty] bool _isFile = false;
    [ObservableProperty] DirectoryTreeItem? _parent;
    [ObservableProperty] Dictionary<string, DirectoryTreeItem> _children = [];

    public DirectoryTreeItem(string name, string fullPath, bool isFile)
    {
        _name = name;
        _fullPath = fullPath;
        _isFile = isFile;
    }

    public int GetFileCount()
    {
        if (IsFile) return 1;
        return Children.Values.Sum(child => child.GetFileCount());
    }

    public void OpenFile(bool isFromBuildPak = false)
    {
        try
        {
            var ext = Path.GetExtension(FullPath).ToLower();
            if (ext == ".uasset" || ext == ".umap")
            {
                MainWindow? window = null;

                foreach (Window w in Application.Current.Windows)
                {
                    if (w is MainWindow mw) window = mw;
                }

                var asset = TryBuildAssetInMemory();
                if (window is null)
                {
                    window = new MainWindow();
                    window.Show();
                }

                window.OpenAssetInEditor(asset, FullPath, isFromBuildPak);
            }
            else
            {
                // non-asset types: still open via shell, prefer disk path if exists
                var diskPath = !string.IsNullOrEmpty(FixedPathOnDisk) ? FixedPathOnDisk : SaveFileToTemp();
                if (string.IsNullOrEmpty(diskPath))
                {
                    MessageBox.Show("Unable to open file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Process.Start(new ProcessStartInfo(diskPath) { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private UAsset TryBuildAssetInMemory()
    {
        // 1) from staged disk file
        if (!string.IsNullOrEmpty(FixedPathOnDisk) && File.Exists(FixedPathOnDisk))
        {
            return new UAsset(FixedPathOnDisk, UAGConfig.Data.PreferredVersion);
        }

        // 2) from GameFiles array
        if (GameFiles.Count == 0)
            throw new FileNotFoundException("No game files available for this asset.");

        // Find the main asset file (.uasset)
        var uassetFile = GameFiles.FirstOrDefault(f =>
            f.Extension.Equals("uasset", StringComparison.OrdinalIgnoreCase) ||
            f.Extension.Equals("umap", StringComparison.OrdinalIgnoreCase)) ??
            throw new FileNotFoundException("Asset file (.uasset) not found in game files.");

        var uasset = uassetFile.Read();
        var uexpFile = GameFiles.FirstOrDefault(f => f.Extension.Equals("uexp", StringComparison.OrdinalIgnoreCase));
        var ubulkFile = GameFiles.FirstOrDefault(f => f.Extension.Equals("ubulk", StringComparison.OrdinalIgnoreCase));

        bool hasUexp = uexpFile != null;
        bool hasUbulk = ubulkFile != null;

        // Create combined stream like UAssetAPI.PathToStream does
        var combinedStream = new MemoryStream();
        combinedStream.Write(uasset, 0, uasset.Length);

        if (hasUexp)
        {
            var uexp = uexpFile!.Read();
            combinedStream.Write(uexp, 0, uexp.Length);
        }
        if (hasUbulk)
        {
            var ubulk = ubulkFile!.Read();
            combinedStream.Write(ubulk, 0, ubulk.Length);
        }
        combinedStream.Seek(0, SeekOrigin.Begin);

        using var reader = new AssetBinaryReader(combinedStream, hasUexp);
        return new UAsset(reader, UAGConfig.Data.PreferredVersion, null, hasUexp || hasUbulk);
    }

    public string? SaveFileToTemp(string? outputPathDirectory = null)
    {
        outputPathDirectory ??= Path.Combine(Path.GetTempPath(), "UAG_read_only",
            Path.GetFileNameWithoutExtension(FullPath));

        Directory.CreateDirectory(outputPathDirectory!);

        if (!string.IsNullOrEmpty(FixedPathOnDisk))
        {
            // Copy from disk files
            var mainFile = GameFiles.FirstOrDefault(f => f.Extension.Equals(".uasset", StringComparison.OrdinalIgnoreCase));
            if (mainFile != null)
            {
                var outputPath = Path.Combine(outputPathDirectory, mainFile.Name);
                File.Copy(FixedPathOnDisk, outputPath, true);

                foreach (var gameFile in GameFiles)
                {
                    if (gameFile != mainFile)
                    {
                        var extPath = Path.Combine(outputPathDirectory, gameFile.Name);
                        try { File.Copy(Path.ChangeExtension(FixedPathOnDisk, gameFile.Extension), extPath, true); } catch { }
                    }
                }
                return outputPath;
            }
        }

        // Use GameFiles array
        if (GameFiles.Count > 0)
        {
            try
            {
                string? mainOutputPath = null;
                foreach (var gameFile in GameFiles)
                {
                    var outputPath = Path.Combine(outputPathDirectory, gameFile.Name);
                    var data = gameFile.Read();
                    if (data != null)
                    {
                        if (File.Exists(outputPath)) { try { File.Delete(outputPath); } catch { } }
                        File.WriteAllBytes(outputPath, data);

                        // Return the main asset file path
                        if (gameFile.Extension.Equals(".uasset", StringComparison.OrdinalIgnoreCase))
                        {
                            mainOutputPath = outputPath;
                        }
                    }
                }
                return mainOutputPath;
            }
            catch { return null; }
        }

        return null;
    }

    [RelayCommand] void ExpandAll() => SetExpandedRecursive(true);
    [RelayCommand] void CollapseAll() => SetExpandedRecursive(false);

    private void SetExpandedRecursive(bool expanded)
    {
        IsExpanded = expanded;
        foreach (var kv in Children)
        {
            kv.Value.SetExpandedRecursive(expanded);
        }
    }
}

public class DirectoryTree
{
    public Dictionary<string, DirectoryTreeItem> RootNodes { get; } = new();

    public DirectoryTree()
    {
    }

    public DirectoryTree(string[] paths, string[]? fixedAssetsOnDisk = null, string? prefix = null)
    {
        if (fixedAssetsOnDisk != null && fixedAssetsOnDisk.Length == paths.Length)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                CreateNode(paths[i], fixedAssetsOnDisk[i], prefix);
            }
        }
        else
        {
            for (int i = 0; i < paths.Length; i++)
            {
                CreateNode(paths[i], null, prefix);
            }
        }
    }

    public DirectoryTreeItem? GetRootNode(string component)
    {
        return RootNodes.TryGetValue(component, out var node) ? node : null;
    }

    public DirectoryTreeItem? GetNode(string path)
    {
        string[] pathComponents = path.Split('/');
        if (pathComponents.Length == 0) return null;
        if (!RootNodes.ContainsKey(pathComponents[0])) return null;

        DirectoryTreeItem currentItem = RootNodes[pathComponents[0]];
        for (int i = 1; i < pathComponents.Length; i++)
        {
            if (!currentItem.Children.ContainsKey(pathComponents[i])) return null;
            currentItem = currentItem.Children[pathComponents[i]];
        }
        return currentItem;
    }

    public DirectoryTreeItem? CreateNode(string path, string? fixedAssetOnDisk = null, string? prefix = null, GameFile? gameFile = null, List<GameFile>? gameFiles = null)
    {
        string[] pathComponents = path.Split('/');
        if (pathComponents.Length == 0) return null;

        string[] fixedAssetOnDiskComponents = fixedAssetOnDisk?.Split(Path.DirectorySeparatorChar) ?? Array.Empty<string>();
        string startingFixedAssetOnDisk = string.Empty;
        if (fixedAssetOnDiskComponents.Length > pathComponents.Length)
        {
            string[] fixedAssetOnDiskComponentsNuevo = new string[fixedAssetOnDiskComponents.Length - pathComponents.Length];
            Array.Copy(fixedAssetOnDiskComponents, fixedAssetOnDiskComponentsNuevo, fixedAssetOnDiskComponentsNuevo.Length);
            startingFixedAssetOnDisk = string.Join(Path.DirectorySeparatorChar, fixedAssetOnDiskComponentsNuevo);
        }

        if (!RootNodes.TryGetValue(pathComponents[0], out DirectoryTreeItem? currentItem))
        {
            string ext = Path.GetExtension(pathComponents[0]);
            if (ext.Length > 1 && (ext == ".uexp" || ext == ".ubulk" || ext == ".bak")) return null;
            currentItem = new DirectoryTreeItem(pathComponents[0], pathComponents[0], ext.Length > 1);
            RootNodes[pathComponents[0]] = currentItem;
        }

        if (startingFixedAssetOnDisk.Length > 0)
        {
            startingFixedAssetOnDisk = Path.Combine(startingFixedAssetOnDisk, pathComponents[0]);
            currentItem.FixedPathOnDisk = startingFixedAssetOnDisk;
        }

        for (int i = 1; i < pathComponents.Length; i++)
        {
            string ext = Path.GetExtension(pathComponents[i]);
            if (ext.Length > 1 && (ext == ".uexp" || ext == ".ubulk" || ext == ".bak")) return null;

            if (!currentItem.Children.ContainsKey(pathComponents[i]))
            {
                currentItem.Children[pathComponents[i]] = new DirectoryTreeItem(
                    pathComponents[i],
                    currentItem.FullPath + "/" + pathComponents[i],
                    ext.Length > 1);
                currentItem.Children[pathComponents[i]].Parent = currentItem;
            }
            currentItem = currentItem.Children[pathComponents[i]];

            if (startingFixedAssetOnDisk.Length > 0)
            {
                startingFixedAssetOnDisk = Path.Combine(startingFixedAssetOnDisk, pathComponents[i]);
                currentItem.FixedPathOnDisk = startingFixedAssetOnDisk;
            }
        }

        // Set GameFile and GameFiles if provided and this is a file
        if (currentItem.IsFile)
        {
            if (gameFile != null)
            {
                currentItem.GameFiles = [gameFile];
            }
            else if (gameFiles != null && gameFiles.Count > 0)
            {
                currentItem.GameFiles = gameFiles;
            }
        }

        return currentItem;
    }
}