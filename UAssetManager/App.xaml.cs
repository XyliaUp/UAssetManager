using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Serilog;
using UAssetManager.Views;

namespace UAssetManager;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        if (e.Exception is COMException) return; // Ignore exceptions

		Log.Error(e.Exception, "UnhandledException");
        MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}