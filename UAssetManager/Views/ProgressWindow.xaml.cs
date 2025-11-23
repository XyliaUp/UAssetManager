using System.ComponentModel;
using System.Windows;
using UAssetManager.Utils.Plugins;

namespace UAssetManager.Views;
public partial class ProgressWindow : Window
{
    private bool _isCancelled = false;
    private BackgroundWorker? _backgroundWorker;

    public ProgressWindow()
    {
        InitializeComponent();
    }

    public bool IsCancelled => _isCancelled;

    public int Maximum
    {
        get => (int)ProgressBar.Maximum;
        set
        {
            ProgressBar.Maximum = value;
        }
    }

    public void SetBackgroundWorker(BackgroundWorker worker)
    {
        _backgroundWorker = worker;
    }

    public void UpdateProgress(int value, string statusText)
    {
        ProgressBar.Value = value;
        ProgressLabel.Text = statusText;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _isCancelled = true;
        
        if (_backgroundWorker != null && _backgroundWorker.WorkerSupportsCancellation)
        {
            _backgroundWorker.CancelAsync();
        }

        ProgressLabel.Text = "Cancelling...";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isCancelled)
        {
            Cancel_Click(this, new RoutedEventArgs());
        }

        base.OnClosing(e);
    }
}

internal class ProgressReporter : IProgressReporter
{
    private readonly ProgressWindow _window;
    private bool _disposed;

    public bool IsCancelled => _window.IsCancelled;

    public ProgressReporter(string title, int total)
    {
        _window = new ProgressWindow
        {
            Title = title,
            Owner = Application.Current.MainWindow,
            Maximum = total
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            _window.Show();
        });
    }

    public void Report(int current, string message)
    {
        if (_disposed) return;

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            _window.UpdateProgress(current, message);
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Application.Current.Dispatcher.Invoke(() =>
        {
            _window?.Close();
        });
    }
}