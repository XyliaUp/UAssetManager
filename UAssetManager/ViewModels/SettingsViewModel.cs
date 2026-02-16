using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UAssetAPI;
using UAssetAPI.UnrealTypes;
using UAssetManager.Models;
using UAssetManager.Resources;
using UAssetManager.Resources.Themes;

namespace UAssetManager.ViewModels;
internal partial class SettingsViewModel : ObservableObject
{
    // Store original config for cancel functionality
    private UAGConfigData? _originalConfig;
    public UAGConfigData Config => UAGConfig.Data;
    public ObservableCollection<SerializationFlagItem> CustomSerializationFlags { get; } = new();

    public IEnumerable<ThemeType> Themes => Enum.GetValues<ThemeType>();
    public IEnumerable<ELanguage> Languages => Enum.GetValues<ELanguage>();
    public IEnumerable<EngineVersion> EngineVersions => Enum.GetValues<EngineVersion>();

    #region Methods
    public SettingsViewModel()
    {
        CloneCurrentConfig();
        RefreshCustomSerializationFlags();
    }

    /// <summary>
    /// Clone current config for cancel functionality
    /// </summary>
    private void CloneCurrentConfig()
    {
        // Clone the main config (includes CustomSerializationFlags)
        _originalConfig = Config.Clone() as UAGConfigData;
    }

    public void RestoreOriginalConfig()
    {
        if (_originalConfig != null)
        {
            // Restore main config (includes CustomSerializationFlags)
            UAGConfig.Data = _originalConfig;

            // Refresh custom serialization flags from restored config
            RefreshCustomSerializationFlags();
        }
    }

    private void RefreshCustomSerializationFlags()
    {
        CustomSerializationFlags.Clear();

        foreach (var flag in Enum.GetValues<CustomSerializationFlags>())
        {
            if (flag == 0) continue;

            CustomSerializationFlags.Add(new SerializationFlagItem
            {
                Name = flag.ToString(),
                Flag = flag,
                IsChecked = (UAGConfig.Data.CustomSerializationFlags & (int)flag) > 0
            });
        }
    }

    public void SaveCustomSerializationFlags()
    {
        // Save custom serialization flags
        int flags = 0;
        foreach (var flagItem in CustomSerializationFlags)
        {
            if (flagItem.IsChecked)
            {
                flags |= (int)flagItem.Flag;
            }
        }
        UAGConfig.Data.CustomSerializationFlags = flags;
    }

    [RelayCommand]
    void ResetToDefaults()
    {
        var result = MessageBox.Show(StringHelper.Get("Confirm.ResetSettings"), "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            UAGConfig.ResetToDefaults();
            RefreshCustomSerializationFlags();
            CloneCurrentConfig();
            OnPropertyChanged(nameof(Config));
        }
    }
    #endregion
}