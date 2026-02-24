using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetManager.Controls;
using UAssetManager.Models;
using UAssetManager.Resources;
using UAssetManager.ViewModels;

namespace UAssetManager.Views;
public partial class MainWindow : Window
{
    #region Constuctor
    private readonly MainWindowViewModel _viewModel;
    private readonly Dictionary<object, WeakReference<TreeNodeItem>> _objectIndex = new(ReferenceEqualityComparer.Instance);

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new MainWindowViewModel();

        SetupKeyboardShortcuts();
        LoadConfiguration();
        SetupPluginMenu();

        // Initialize Discord RPC
        _viewModel.UpdateDiscordRpc();

        // Fetch the latest version from github
        Task.Run(GitHubAPI.CheckForUpdates);
    }

    private void SetupPluginMenu()
    {
        // Subscribe to plugin changes
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Plugins))
            {
                UpdatePluginMenuItems();
            }
        };

        // Initial setup
        UpdatePluginMenuItems();
    }

    private void UpdatePluginMenuItems()
    {
        if (this.FindName("PluginsMenu") is not MenuItem pluginsMenu || 
            pluginsMenu.FindName("PluginSeparator") is not Separator pluginSeparator) return;

        // Remove old plugin items (keep management items)
        var itemsToRemove = pluginsMenu.Items.Cast<object>().Skip(3).ToList(); // Skip first 3 items (Open Folder, Reload, Separator)
        foreach (var item in itemsToRemove)
        {
            pluginsMenu.Items.Remove(item);
        }

        // Show/hide separator based on plugin count
        pluginSeparator.Visibility = _viewModel.Plugins.Any() ? Visibility.Visible : Visibility.Collapsed;

        // Add plugin menu items
        foreach (var plugin in _viewModel.Plugins)
        {
            var customMenuItems = plugin.GetMenuItems();
            if (customMenuItems != null && customMenuItems.Any())
            {
                // Create submenu for plugin with custom items
                var pluginMenuItem = new MenuItem
                {
                    Header = plugin.Name,
                    ToolTip = $"Author: {plugin.Author}\nVersion: {plugin.Version}\n{plugin.Description}"
                };

                // Add main execute item
                pluginMenuItem.Items.Add(new MenuItem
                {
                    Header = "Execute",
                    Command = _viewModel.ExecutePluginCommand,
                    CommandParameter = plugin,
                    FontWeight = FontWeights.Bold
                });

                // Add separator
                pluginMenuItem.Items.Add(new Separator());

                // Add custom menu items
                foreach (var customItem in customMenuItems)
                {
                    pluginMenuItem.Items.Add(new MenuItem
                    {
                        Header = customItem.Header,
                        Command = new RelayCommand(() => customItem.Action()),
                        ToolTip = customItem.ToolTip
                    });
                }

                ToolTipService.SetInitialShowDelay(pluginMenuItem, 100);
                pluginsMenu.Items.Add(pluginMenuItem);
            }
            else
            {
                // No custom items, add simple menu item
                var pluginMenuItem = new MenuItem
                {
                    Header = plugin.Name,
                    Command = _viewModel.ExecutePluginCommand,
                    CommandParameter = plugin,
                    ToolTip = $"Author: {plugin.Author}\nVersion: {plugin.Version}\n{plugin.Description}"
                };

                ToolTipService.SetInitialShowDelay(pluginMenuItem, 100);
                pluginsMenu.Items.Add(pluginMenuItem);
            }
        }
    }
    #endregion

    #region Methods

    private void LoadConfiguration()
    {
        try
        {
            // Load configuration
            UAGConfig.Load();

			// Apply saved settings
			Width = UAGConfig.Data.StartupWidth;
			Height = UAGConfig.Data.StartupHeight;
        }
        catch (Exception ex)
        {
			Debug.WriteLine($"Error loading configuration: {ex.Message}");
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            // Save current settings
            UAGConfig.Data.StartupWidth = (int)Width;
            UAGConfig.Data.StartupHeight = (int)Height;
            UAGConfig.Save();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    private void SetupKeyboardShortcuts()
    {
        // Setup keyboard shortcuts
        InputBindings.Add(new KeyBinding(_viewModel.OpenFileCommand, new KeyGesture(Key.O, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_viewModel.OpenContainerCommand, new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift)));
        InputBindings.Add(new KeyBinding(_viewModel.SaveFileCommand, new KeyGesture(Key.S, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_viewModel.SaveFileAsCommand, new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)));
        InputBindings.Add(new KeyBinding(_viewModel.FindCommand, new KeyGesture(Key.F, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_viewModel.RefreshCommand, new KeyGesture(Key.F5)));
        InputBindings.Add(new KeyBinding(_viewModel.CopyCommand, new KeyGesture(Key.C, ModifierKeys.Control)));
        InputBindings.Add(new KeyBinding(_viewModel.PasteCommand, new KeyGesture(Key.V, ModifierKeys.Control)));
        CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, PasteData));
    }

    private void UpdateStatus(string message)
    {
        _viewModel.Status = message;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_viewModel.HasUnsavedChanges && MessageBox.Show(
            StringHelper.Get("MainWindow_FileModifiedConfirmMessage"),
            StringHelper.Get("MainWindow_ConfirmTitle"),
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
            _viewModel.SaveFile();

        // Save configuration before closing
        SaveConfiguration();

        // Dispose Discord RPC
        _viewModel.DisposeDiscordRpc();

        base.OnClosing(e);
    }

    public void OpenAssetInEditor(UAsset asset, string virtualPath, bool isFromBuildPak = false)
    {
        _viewModel.SetCurrentAsset(asset, virtualPath, isFromBuildPak);
        Activate();
        Focus();
    }

    public void OpenFileInEditor(string filePath)
    {
        _viewModel.LoadFile(filePath);
        Activate();
        Focus();
    }

    #endregion

    #region Data Grid

    private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        // Check if current cell can be edited
        var header = e.Column.Header.ToString();
        if (header == "Index")
        {
            e.Cancel = true;
            return;
        }

        // For other types (like ExportItem, ImportItem, etc.), allow editing
        UpdateStatus(StringHelper.Get("MainWindow_Status.Editing"));
    }

    private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel)
        {
            UpdateStatus("Edit cancelled");
            return;
        }

        try
        {
            // Get edited value
            string newValue = string.Empty;
            if (e.EditingElement is TextBox textBox)
            {
                newValue = textBox.Text;
            }
            else if (e.EditingElement is ComboBox comboBox)
            {
                newValue = comboBox.SelectedItem?.ToString() ?? string.Empty;
            }
            else if (e.EditingElement is CheckBox checkBox)
            {
                newValue = checkBox.IsChecked.ToString() ?? string.Empty;
            }

            // Handle editing based on data type
            bool editSuccess = false;
            switch (e.Row.Item)
            {
                case ExportItem exportItem:
                    editSuccess = exportItem.HandlePropertyEdit(e.Column.Header.ToString(), newValue);
                    break;
                case ImportItem importItem:
                    editSuccess = importItem.HandlePropertyEdit(e.Column.Header.ToString(), newValue);
                    break;
                case NameMapItem nameMapItem:
                    editSuccess = nameMapItem.HandlePropertyEdit(e.Column.Header.ToString(), newValue);
                    break;
                default:
                    e.Cancel = true;
                    UpdateStatus(StringHelper.Get("MainWindow_InvalidCellMessage"));
                    return;
            }

            // check result
            if (editSuccess)
            {
                _viewModel.HasUnsavedChanges = true;
                UpdateStatus("File modified");
            }
            else
            {
                MessageBox.Show(StringHelper.Get("MainWindow_InvalidColumnValueMessage", e.Column.Header, newValue), StringHelper.Get("MainWindow_EditErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(StringHelper.Get("MainWindow_CellEditErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Edit failed");
        }
    }

    private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
    {
        // Create new item based on current data context type
        switch (_viewModel.CurrentDataContext)
        {
            case ObservableCollection<ImportItem>:
                e.NewItem = _viewModel.AddImport();
                UpdateStatus("Added new import item");
                break;

            case ObservableCollection<ExportItem>:
                e.NewItem = _viewModel.AddExport();
                UpdateStatus("Added new export item");
                break;
        }
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction == DataGridEditAction.Cancel)
        {
            UpdateStatus("Row edit cancelled");
            return;
        }

        try
        {
            // Handle row edit ending
            if (e.Row.Item is ImportItem importItem)
            {
                // ImportItem editing has already been handled in CellEditEnding
                _viewModel.HasUnsavedChanges = true;
                UpdateStatus("Import item updated");
            }
            else if (e.Row.Item is ExportItem exportItem)
            {
                // ExportItem editing has already been handled in CellEditEnding
                _viewModel.HasUnsavedChanges = true;
                UpdateStatus("Export item updated");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(StringHelper.Get("MainWindow_UpdateRowErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Failed to update row");
        }
    }


    private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && DataGridView.SelectedItems.Count > 0)
        {
            try
            {
                var itemsToRemove = DataGridView.SelectedItems;

                if (MessageBox.Show(StringHelper.Get("Confirm.DeleteItems", itemsToRemove.Count), StringHelper.Get("MainWindow_ConfirmDeleteTitle"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    var deleted = _viewModel.DeleteItems(itemsToRemove);
                    UpdateStatus(deleted > 0 ? $"Deleted {deleted} items" : "Nothing deleted");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(StringHelper.Get("MainWindow_DeleteItemsErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                UpdateStatus("Failed to delete items");
            }

            e.Handled = true;
        }
    }

    private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
    {
        if (e.Row.Item is Models.PropertyItem propertyItem && propertyItem.Type == "PrettyKismet")
        {
            // Apply special styling for PrettyKismet type
            e.Row.Height = 200; // Set fixed height for pretty bytecode display
        }
    }

    private void RemoveSelected(object sender, RoutedEventArgs e)
    {
        try
        {
            var itemsToRemove = DataGridView.SelectedItems;
            if (itemsToRemove.Count == 0) return;
            if (MessageBox.Show(StringHelper.Get("Confirm.DeleteItems", itemsToRemove.Count), StringHelper.Get("MainWindow_ConfirmDeleteTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            var deleted = _viewModel.DeleteItems(itemsToRemove);
            UpdateStatus(deleted > 0 ? $"Deleted {deleted} items" : "Nothing deleted");
        }
        catch (Exception ex)
        {
            MessageBox.Show(StringHelper.Get("MainWindow_DeleteItemsErrorMessage", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PasteData(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if there is a selected row
            var selectedItem = DataGridView.SelectedItem;
            if (selectedItem == null)
            {
                UpdateStatus(StringHelper.Get("MainWindow_Paste_SelectRowFirst"));
                return;
            }

            // If selected item is new item placeholder, manually create new item
            if (selectedItem == CollectionView.NewItemPlaceholder)
            {
                switch (_viewModel.CurrentDataContext)
                {
                    case ObservableCollection<ImportItem>: selectedItem = _viewModel.AddImport(true); break;
                    case ObservableCollection<ExportItem>: selectedItem = _viewModel.AddExport(true); break;
                    default: UpdateStatus(StringHelper.Get("MainWindow_Paste_DataSourceNotSupported")); return;
                }
            }

            // Get clipboard text
            string clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                UpdateStatus(StringHelper.Get("MainWindow_Paste_NoDataToPaste"));
                return;
            }

            // Split data by tab character
            var pasteData = clipboardText.Split('\t');
            if (pasteData.Length == 0 || pasteData.Length != DataGridView.Columns.Count)
            {
                UpdateStatus(StringHelper.Get("MainWindow_Paste_InvalidDataFormat"));
                return;
            }

            // Write data to corresponding columns in order
            int pastedFields = 0;
            for (int i = 0; i < DataGridView.Columns.Count; i++)
            {
                var column = DataGridView.Columns[i];
                if (column.Header is null or "Index") continue; // skip

                var editSuccess = selectedItem switch
                {
                    ExportItem exportItem => exportItem.HandlePropertyEdit(column.Header.ToString(), pasteData[i]),
                    ImportItem importItem => importItem.HandlePropertyEdit(column.Header.ToString(), pasteData[i]),
                    _ => false
                };

                if (editSuccess) pastedFields++;
            }

            if (pastedFields == 0) throw new Exception(StringHelper.Get("MainWindow_Paste_UnableToUpdateData"));

            _viewModel.HasUnsavedChanges = true;
            UpdateStatus(StringHelper.Get("MainWindow_Paste_Success", pastedFields));
            DataGridView.Items.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(StringHelper.Get("MainWindow_Paste_Failed", ex.Message), StringHelper.Get("MainWindow_ErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus(StringHelper.Get("MainWindow_Paste_FailedStatus"));
        }
    }

    #endregion

    #region Tree View

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var node = _viewModel.SelectedTreeNode = (TreeNodeItem)e.NewValue;
        if (node is PointingTreeNodeItem ptn && ptn.Data is NormalExport normalExport)
        {
            DataGridView.Visibility = Visibility.Collapsed;
            PropertyEditor.Visibility = Visibility.Visible;
            PropertyEditor.SetCurrentValue(PropertyEditor.SourceProperty, normalExport.Data);
            return;
        }

        // Find the nearest parent PointingTreeNodeItem whose Data is ArrayPropertyData or StructPropertyData
        PropertyData? propertyCtx = null;
        TreeNodeItem? cur = node;
        while (cur != null)
        {
            if (cur is PointingTreeNodeItem ptn2 && ptn2.Data is PropertyData prop)
            {
                propertyCtx = prop;
                break;
            }
            cur = cur.Parent;
        }

        if (propertyCtx != null)
        {
            // Check property type to decide which view to use
            if (propertyCtx is ArrayPropertyData arrayProp && arrayProp.Value.Length > 0)
            {
                DataGridView.Visibility = Visibility.Collapsed;
                PropertyEditor.Visibility = Visibility.Visible;
                PropertyEditor.SetCurrentValue(PropertyEditor.SourceProperty, arrayProp.Value);
                return;
            }
            else if (propertyCtx is StructPropertyData structProp && structProp.Value.Count > 0)
            {
                DataGridView.Visibility = Visibility.Collapsed;
                PropertyEditor.Visibility = Visibility.Visible;
                PropertyEditor.SetCurrentValue(PropertyEditor.SourceProperty, structProp.Value);
                return;
            }
            else
            {
                // 处理单个PropertyData，创建一个包含它的集合
                DataGridView.Visibility = Visibility.Collapsed;
                PropertyEditor.Visibility = Visibility.Visible;
                PropertyEditor.SetCurrentValue(PropertyEditor.SourceProperty, new[] { propertyCtx });
                return;
            }
        }

        // Other cases use DataGrid, maintain original CurrentDataContext
        PropertyEditor.Visibility = Visibility.Collapsed;
        DataGridView.Visibility = Visibility.Visible;
        PropertyEditor.ClearValue(PropertyEditor.SourceProperty);
    }

    public void SelectNode(TreeNodeItem target)
    {
        if (target == null) return;
        ExpandAncestors(target);
        ApplySelection(target);
    }

    public bool SelectByObject(object target)
    {
        if (target == null) return false;
        if (_objectIndex.TryGetValue(target, out var wr) && wr.TryGetTarget(out var cached))
        {
            SelectNode(cached);
            return true;
        }
        foreach (var root in _viewModel.TreeNodes)
        {
            if (SelectByObjectFrom(root, target)) return true;
        }
        return false;
    }

    private bool SelectByObjectFrom(TreeNodeItem node, object target)
    {
        RegisterNodeObjectMapping(node);
        // Match against data
        if (node is PointingTreeNodeItem p)
        {
            if (ReferenceEquals(p.Data, target))
            {
                SelectNode(node);
                return true;
            }
        }
        if (node.Data != null && ReferenceEquals(node.Data, target))
        {
            SelectNode(node);
            return true;
        }

        // Ensure children are present for dynamic nodes, then traverse
        node.Materialize();

        foreach (var child in node.Children)
        {
            if (SelectByObjectFrom(child, target)) return true;
        }
        return false;
    }

    private void ExpandAncestors(TreeNodeItem target)
    {
        var stack = new Stack<TreeNodeItem>();
        var cur = target.Parent;
        while (cur != null)
        {
            stack.Push(cur);
            cur = cur.Parent;
        }
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            n.IsExpanded = true;
            n.Materialize();
            TreeView1.UpdateLayout();
        }
        TreeView1.UpdateLayout();
    }

    private void ApplySelection(TreeNodeItem target)
    {
        _viewModel.SelectedTreeNode = target;
        target.IsSelected = true;
        TreeView1.UpdateLayout();
        var tvi = GetTreeViewItem(TreeView1, target);
        if (tvi != null)
        {
            tvi.BringIntoView();
            tvi.Focus();
            // ensure after layout
            Dispatcher.InvokeAsync(() =>
            {
                tvi.BringIntoView();
                tvi.Focus();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void RegisterNodeObjectMapping(TreeNodeItem node)
    {
        if (node is PointingTreeNodeItem p && p.Data != null)
        {
            _objectIndex[p.Data] = new WeakReference<TreeNodeItem>(node);
        }
        if (node.Data != null)
        {
            _objectIndex[node.Data] = new WeakReference<TreeNodeItem>(node);
        }
    }

    private static TreeViewItem? GetTreeViewItem(ItemsControl container, object item)
    {
        if (container == null) return null;
        if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem direct) return direct;

        foreach (var child in container.Items)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(child) is not TreeViewItem childContainer) continue;
            // Ensure generated
            childContainer.UpdateLayout();
            var result = GetTreeViewItem(childContainer, item);
            if (result != null) return result;
        }
        return null;
    }

    #endregion
}