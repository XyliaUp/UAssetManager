using System.Windows;
using System.Windows.Input;
using UAssetManager.Models;
using UAssetManager.ViewModels;

namespace UAssetManager.Views;
public partial class FindWindow : Window
{
    readonly FindViewModel _viewModel;

    public FindWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new FindViewModel();
        _viewModel.CloseRequest += (s, e) => Close();

        // Set focus to search box and attach context
        Loaded += (s, e) =>
        {
            SearchForTextBox.Focus();
            if (Owner is FrameworkElement mw && mw.DataContext is ITreeSearchProvider provider)
            {
                _viewModel.Initialize(provider);
            }
            else
            {
                MessageBox.Show("未找到主窗口上下文", "错误", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        };

        InputBindings.Add(new KeyBinding(_viewModel.CloseCommand, new KeyGesture(Key.Escape)));
        InputBindings.Add(new KeyBinding(_viewModel.FindPreviousCommand, new KeyGesture(Key.F3, ModifierKeys.Shift)));
        InputBindings.Add(new KeyBinding(_viewModel.FindNextCommand, new KeyGesture(Key.F3, ModifierKeys.None)));
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.ClearHighlights();
        base.OnClosed(e);
    }

    private void SearchResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectedResult != null)
        {
            var data = _viewModel.SelectedResult;
            if (data is TreeNodeItem node)
            {
                if (Owner is MainWindow mw)
                {
                    mw.SelectNode(node);
                }
            }
            else if (data != null)
            {
                // data may be the underlying model object; try to locate dynamically
                if (Owner is MainWindow mw)
                {
                    mw.SelectByObject(data);
                }
            }
            Close();
        }
    }
}