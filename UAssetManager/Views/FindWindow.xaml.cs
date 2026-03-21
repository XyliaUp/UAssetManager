using System.Windows;
using System.Windows.Input;
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
                throw new InvalidOperationException("Failed to find main window.");
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
}