using System.Windows;
using UAssetManager.ViewModels;

namespace UAssetManager.Views;
public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        DataContext = new AboutWindowViewModel();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}