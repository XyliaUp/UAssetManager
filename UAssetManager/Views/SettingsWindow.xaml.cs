using System.Windows;
using UAssetManager.Models;
using UAssetManager.Resources;
using UAssetManager.ViewModels;

namespace UAssetManager.Views;
public partial class SettingsWindow : Window
{
    private SettingsViewModel _viewModel;

    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new SettingsViewModel();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Save custom serialization flags
            _viewModel.SaveCustomSerializationFlags();
            UAGConfig.Save();

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(StringHelper.Get("Settings_SaveErrorMessage", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        // Restore original config
        _viewModel.RestoreOriginalConfig();
        DialogResult = false;
        Close();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow();
        aboutWindow.Owner = this;
        aboutWindow.ShowDialog();
    }
}