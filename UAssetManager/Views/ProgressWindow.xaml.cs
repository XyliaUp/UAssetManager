using System.ComponentModel;
using System.Windows;

namespace UAssetManager.Views;
public partial class ProgressWindow : Window
{
    private bool _isCancelled = false;
    private BackgroundWorker? _backgroundWorker;

    public ProgressWindow()
    {
        InitializeComponent();
    }

    public int Value
    {
        get => (int)ProgressBar.Value;
        set
        {
            ProgressBar.Value = value;
            UpdateProgressText();
        }
    }

    public int Maximum
    {
        get => (int)ProgressBar.Maximum;
        set
        {
            ProgressBar.Maximum = value;
            UpdateProgressText();
        }
    }

    public string StatusText
    {
        get => ProgressLabel.Content?.ToString() ?? string.Empty;
        set => ProgressLabel.Content = value;
    }

    public bool IsCancelled => _isCancelled;

    public void SetBackgroundWorker(BackgroundWorker worker)
    {
        _backgroundWorker = worker;
    }

    public void UpdateProgress(int value)
    {
        Value = value;
    }

    public void UpdateProgress(int value, string statusText)
    {
        Value = value;
        StatusText = statusText;
    }

    private void UpdateProgressText()
    {
        ProgressText.Text = $"{Value}/{Maximum}";
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _isCancelled = true;
        
        if (_backgroundWorker != null && _backgroundWorker.WorkerSupportsCancellation)
        {
            _backgroundWorker.CancelAsync();
        }
        
        StatusText = "Cancelling...";
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
