using Serilog;
using System.IO;
using System.Reflection;
using UAssetAPI.Pak.Pak.Utils;
using UAssetManager.Resources;

namespace UAssetManager.Utils;
/// <summary>
/// Plugin loader - manages plugin lifecycle
/// </summary>
public class PluginLoader
{
    private readonly string _pluginDirectory;
    private readonly List<IUAssetPlugin> _plugins = [];
    private IPluginHost? _pluginHost;

    public IReadOnlyCollection<IUAssetPlugin> Plugins => _plugins;

    public PluginLoader(string pluginDirectory)
    {
        _pluginDirectory = pluginDirectory;
        
        // Setup assembly resolver for plugin dependencies
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    /// <summary>
    /// Get plugin directory path
    /// </summary>
    public string GetPluginDirectory()
    {
        return _pluginDirectory;
    }

    /// <summary>
    /// Load all plugins
    /// </summary>
    public void LoadPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            Directory.CreateDirectory(_pluginDirectory);
            Log.Information(StringHelper.Get("Plugin_CreatedDirectory", _pluginDirectory));
            return;
        }

        var pluginFiles = Directory.GetFiles(_pluginDirectory, "*.dll", SearchOption.AllDirectories);

        foreach (var pluginFile in pluginFiles)
        {
            try
            {
                LoadPlugin(pluginFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, StringHelper.Get("Plugin_FailedToLoad", pluginFile));
            }
        }

        Log.Information(StringHelper.Get("Plugin_LoadedCount", _plugins.Count));
    }

    /// <summary>
    /// Load plugin
    /// </summary>
    private void LoadPlugin(string pluginPath)
    {
        try
        {
            var assembly = Assembly.LoadFrom(pluginPath);

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Log detailed information about which types failed to load
                Log.Warning(StringHelper.Get("Plugin_TypesLoadWarning", Path.GetFileName(pluginPath)));
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    if (loaderException != null)
                    {
                        Log.Warning($"  - {loaderException.Message}");
                    }
                }
                
                // Use only the types that loaded successfully
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            var pluginTypes = types
                .Where(t => t != null && typeof(IUAssetPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    var plugin = (IUAssetPlugin?)Activator.CreateInstance(pluginType);
                    if (plugin != null)
                    {
                        _plugins.Add(plugin);
                        Log.Information(StringHelper.Get("Plugin_Loaded", plugin.Name, plugin.Version, plugin.Author));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, StringHelper.Get("Plugin_FailedToInstantiate", pluginType.FullName));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, StringHelper.Get("Plugin_FailedToLoadAssembly", pluginPath));
            throw;
        }
    }

    /// <summary>
    /// Initialize all plugins
    /// </summary>
    public void InitializePlugins(IPluginHost host)
    {
        _pluginHost = host;
        
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Initialize(host);
                Log.Information(StringHelper.Get("Plugin_Initialized", plugin.Name));
            }
            catch (Exception ex)
            {
                Log.Error(ex, StringHelper.Get("Plugin_FailedToInitialize", plugin.Name));
            }
        }
    }

    /// <summary>
    /// Cleanup all plugins
    /// </summary>
    public void CleanupPlugins()
    {
        foreach (var plugin in _plugins)
        {
            try
            {
                plugin.Cleanup();
            }
            catch (Exception ex)
            {
                Log.Error(ex, StringHelper.Get("Plugin_FailedToCleanup", plugin.Name));
            }
        }

        _plugins.Clear();
    }

    /// <summary>
    /// Initialize the plugin system
    /// </summary>
    public void Initialize(IPluginHost host)
    {
        try
        {
            LoadPlugins();
            InitializePlugins(host);
            Log.Information(StringHelper.Get("Plugin_SystemInitialized", _plugins.Count));
        }
        catch (Exception ex)
        {
            Log.Error(ex, StringHelper.Get("Plugin_FailedToInitializeSystem"));
        }
    }

    /// <summary>
    /// Execute a specific plugin
    /// </summary>
    public void ExecutePlugin(IUAssetPlugin plugin)
    {
        try
        {
            Log.Information(StringHelper.Get("Plugin_Executing", plugin.Name));
            plugin.Execute();
        }
        catch (Exception ex)
        {
            Log.Error(ex, StringHelper.Get("Plugin_FailedToExecute", plugin.Name));
            throw;
        }
    }

    /// <summary>
    /// Reload all plugins
    /// </summary>
    public void ReloadPlugins()
    {
        if (_pluginHost == null)
        {
            Log.Warning(StringHelper.Get("Plugin_CannotReload"));
            return;
        }
        
        CleanupPlugins();
        Initialize(_pluginHost);
    }

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        // Try to load from plugin directory
        var assemblyName = new AssemblyName(args.Name);
        var assemblyFileName = assemblyName.Name + ".dll";

        // Search in plugin directory
        var assemblyPath = Path.Combine(_pluginDirectory, assemblyFileName);
        if (File.Exists(assemblyPath))
        {
            Log.Information(StringHelper.Get("Plugin_ResolvingAssemblyFromPluginDir", assemblyFileName));
            return Assembly.LoadFrom(assemblyPath);
        }

        // Search in main application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        assemblyPath = Path.Combine(appDirectory, assemblyFileName);
        if (File.Exists(assemblyPath))
        {
            Log.Information(StringHelper.Get("Plugin_ResolvingAssemblyFromAppDir", assemblyFileName));
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }
}