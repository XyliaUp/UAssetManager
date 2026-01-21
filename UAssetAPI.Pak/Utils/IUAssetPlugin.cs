namespace UAssetAPI.Pak.Pak.Utils;

/// <summary>
/// UAssetManager plugin interface
/// </summary>
public interface IUAssetPlugin
{
    /// <summary>
    /// Plugin name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin description
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Plugin author
    /// </summary>
    string Author { get; }

    /// <summary>
    /// Initialize the plugin
    /// </summary>
    /// <param name="host">Plugin host</param>
    void Initialize(IPluginHost host);

    /// <summary>
    /// Execute the plugin
    /// </summary>
    void Execute();

    /// <summary>
    /// Cleanup the plugin
    /// </summary>
    void Cleanup();

    /// <summary>
    /// Get custom menu items for this plugin (optional)
    /// </summary>
    /// <returns>List of custom menu items, or null if no custom items</returns>
    IEnumerable<PluginMenuItem>? GetMenuItems() => null;
}

/// <summary>
/// Plugin host interface - provides services to plugins
/// </summary>
public interface IPluginHost
{
    /// <summary>
    /// Get the currently loaded UAsset
    /// </summary>
    UAsset? GetCurrentAsset();

    /// <summary>
    /// Load a UAsset file
    /// </summary>
    UAsset? LoadAsset(string path);

    /// <summary>
    /// Save a UAsset file
    /// </summary>
    void SaveAsset(UAsset asset, string path);

    /// <summary>
    /// Show a message dialog
    /// </summary>
    void ShowMessage(string message, string title = "Plugin Message");

    /// <summary>
    /// Show an error dialog
    /// </summary>
    void ShowError(string message, string title = "Plugin Error");

    /// <summary>
    /// Show a progress dialog
    /// </summary>
    IProgressReporter ShowProgress(string title, int total);

    /// <summary>
    /// Get the application config directory
    /// </summary>
    string GetConfigDirectory();
}

/// <summary>
/// Progress reporter interface
/// </summary>
public interface IProgressReporter : IDisposable
{
    /// <summary>
    /// Report progress
    /// </summary>
    void Report(int current, string message);

    /// <summary>
    /// Whether the operation is cancelled
    /// </summary>
    bool IsCancelled { get; }
}

/// <summary>
/// Plugin menu item definition
/// </summary>
public class PluginMenuItem
{
    /// <summary>
    /// Menu item display text
    /// </summary>
    public required string Header { get; init; }

    /// <summary>
    /// Action to execute when menu item is clicked
    /// </summary>
    public required Action Action { get; init; }

    /// <summary>
    /// Optional tooltip text
    /// </summary>
    public string? ToolTip { get; init; }
}