using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using UAssetManager.Resources;
using UAssetManager.Utils;

namespace UAssetManager.Views;

public partial class PluginPackageInstallWindow : Window
{
    private readonly PluginPackageInstaller _installer = new();
    private CancellationTokenSource? _operation;
    private PluginPackageRelease? _release;

    public PluginPackageInstallWindow()
    {
        InitializeComponent();
        RepositoryUrl.TextChanged += RepositoryUrl_Changed;
        UpdateControls();
    }

    private void RepositoryUrl_Changed(object sender, TextChangedEventArgs e)
    {
        ClearRelease();
        SetStatus(string.Empty);
        UpdateControls();
    }

    private async void Find_Click(object sender, RoutedEventArgs e)
    {
        var url = RepositoryUrl.Text.Trim();
        if (url.Length == 0)
            return;

        ClearRelease();
        await RunAsync("PluginInstaller.Finding", "PluginInstaller.FindFailed", async token =>
        {
            var release = await _installer.FindReleaseAsync(url, token);
            _release = release;
            ReleaseVersion.Text = $"{release.Repository}  ·  {release.Version}";
            ReleaseFile.Text = release.ArchiveName;
            ReleaseDetails.Visibility = Visibility.Visible;
            SetStatus(string.Empty);
        });
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (_release is not { } release)
            return;

        await RunAsync("PluginInstaller.Downloading", "PluginInstaller.InstallFailed", async token =>
        {
            var progress = new Progress<PluginPackageProgress>(value => ShowProgress(value, token));
            var result = await _installer.InstallAsync(release, progress, token);
            ClearRelease();
            SetStatus(StringHelper.Get("PluginInstaller.Installed", result.Name, result.Version));
            CloseButton.Focus();
        });
    }

    private void ClearRelease()
    {
        _release = null;
        ReleaseDetails.Visibility = Visibility.Collapsed;
    }

    private async Task RunAsync(string statusKey, string errorKey, Func<CancellationToken, Task> action)
    {
        if (_operation is not null)
            return;

        using var operation = new CancellationTokenSource();
        _operation = operation;
        UpdateControls();
        SetStatus(StringHelper.Get(statusKey));
        DownloadProgress.IsIndeterminate = true;
        DownloadProgress.Visibility = Visibility.Visible;
        try
        {
            await action(operation.Token);
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
            SetStatus(StringHelper.Get("Text.Cancelled"));
        }
        catch (OperationCanceledException)
        {
            SetStatus(StringHelper.Get("PluginInstaller.TimedOut"));
        }
        catch (PluginPackageException error)
        {
            SetStatus(StringHelper.Get(error.ResourceKey, error.ResourceArguments));
        }
        catch (Exception error) when (error is InvalidDataException or HttpRequestException or IOException or UnauthorizedAccessException)
        {
            SetStatus(StringHelper.Get(errorKey, error.Message));
        }
        finally
        {
            _operation = null;
            DownloadProgress.Visibility = Visibility.Collapsed;
            UpdateControls();
        }
    }

    private void SetStatus(string text)
    {
        StatusText.Text = text;
        StatusPanel.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ShowProgress(PluginPackageProgress progress, CancellationToken token)
    {
        if (_operation?.Token != token || progress.TotalBytes <= 0)
            return;

        DownloadProgress.IsIndeterminate = false;
        DownloadProgress.Maximum = progress.TotalBytes;
        DownloadProgress.Value = Math.Clamp(progress.BytesDownloaded, 0, progress.TotalBytes);
    }

    private void UpdateControls()
    {
        var busy = _operation is not null;
        RepositoryUrl.IsEnabled = !busy;
        FindButton.IsEnabled = !busy && !string.IsNullOrWhiteSpace(RepositoryUrl.Text);
        InstallButton.IsEnabled = !busy && _release is not null;
        FindButton.IsDefault = FindButton.IsEnabled && !InstallButton.IsEnabled;
        InstallButton.IsDefault = InstallButton.IsEnabled;
        CloseButton.SetResourceReference(ContentProperty, busy ? "Text.Cancel" : "Text.Close");
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_operation is { } operation)
        {
            e.Cancel = true;
            operation.Cancel();
            SetStatus(StringHelper.Get("PluginInstaller.Cancelling"));
        }

        base.OnClosing(e);
    }
}
