using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UAssetManager.Models;
using UAssetManager.ViewModels;

namespace UAssetManager.Views;
public partial class FileContainerWindow
{
    #region Consturctor
    readonly FileContainerViewModel _viewModel;

    public FileContainerWindow(string? path = null)
    {
        InitializeComponent();
        DataContext = _viewModel = new FileContainerViewModel();

        // Subscribe to asset update events
        MainWindowViewModel.AssetUpdateRequested += OnAssetUpdateRequested;

        // load pak if path provided
        if (!string.IsNullOrEmpty(path)) _viewModel.LoadFromPak(path);
    }

    protected override void OnClosed(EventArgs e)
    {
        // Unsubscribe from events to prevent memory leaks
        MainWindowViewModel.AssetUpdateRequested -= OnAssetUpdateRequested;
        base.OnClosed(e);
    }
    #endregion

    #region Methods
    public void OnAssetUpdateRequested(object? sender, AssetUpdateEventArgs e)
    {
        _viewModel.UpdateAssetInBuildPak(e.GamePath, e.Asset);
    }

    private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeView tree && tree.SelectedItem is DirectoryTreeItem item && item.IsFile)
        {
            // Check if this is from the SaveTree (build PAK)
            item.OpenFile(tree == SaveTreeView);
        }
    }

    private void LoadTreeView_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
    }

    private void LoadTreeView_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                _viewModel.LoadFromPak(files[0]);
            }
        }
    }

    private void SaveTreeView_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
    }

    private void SaveTreeView_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                if (SaveTreeView.SelectedItem is DirectoryTreeItem target)
                {
                    var baseDir = target.IsFile ? target.Parent : target;
                    var desired = System.IO.Path.Combine(baseDir?.FullPath ?? string.Empty, System.IO.Path.GetFileName(files[0]))
                        .Replace(System.IO.Path.DirectorySeparatorChar, '/');
                    _viewModel.AddFromDisk(files[0], desired);
                }
                else
                {
                    _viewModel.AddFromDisk(files[0], null);
                }
            }
        }
    }
    #endregion

    #region Drag and Drop
    private DirectoryTreeItem? _draggedItem;
    private Point _dragStartPoint;

    private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is DirectoryTreeItem item)
        {
            _draggedItem = item;
            _dragStartPoint = e.GetPosition(null);
        }
    }

    private void TreeViewItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedItem != null && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPosition = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                DragDrop.DoDragDrop(sender as DependencyObject, _draggedItem, DragDropEffects.Move);
                _draggedItem = null;
            }
        }
    }

    private void TreeViewItem_DragOver(object sender, DragEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is DirectoryTreeItem targetItem)
        {
            if (_draggedItem != null && _draggedItem != targetItem)
            {
                // 只允许拖拽到文件夹，不允许拖拽到文件
                if (!targetItem.IsFile)
                {
                    e.Effects = DragDropEffects.Move;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        e.Handled = true;
    }

    private void TreeViewItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && treeViewItem.DataContext is DirectoryTreeItem targetItem)
        {
            if (_draggedItem != null && _draggedItem != targetItem && !targetItem.IsFile)
            {
                // 移动文件到目标文件夹
                MoveItemToFolder(_draggedItem, targetItem);
            }
        }
        _draggedItem = null;
        e.Handled = true;
    }

    private void MoveItemToFolder(DirectoryTreeItem sourceItem, DirectoryTreeItem targetFolder)
    {
        if (sourceItem.IsFile)
        {
            // 移动文件
            string newPath = Path.Combine(targetFolder.FullPath, sourceItem.Name).Replace(Path.DirectorySeparatorChar, '/');
            
            // 更新 ViewModel 中的路径
            _viewModel.MoveItem(sourceItem.FullPath, newPath);
        }
        else
        {
            // 移动文件夹
            string folderName = sourceItem.Name;
            string newPath = Path.Combine(targetFolder.FullPath, folderName).Replace(Path.DirectorySeparatorChar, '/');
            
            // 更新 ViewModel 中的路径
            _viewModel.MoveFolder(sourceItem.FullPath, newPath);
        }
    }
    #endregion
}