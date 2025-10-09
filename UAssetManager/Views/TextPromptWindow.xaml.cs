using System.Windows;

namespace UAssetManager.Views;
public partial class TextPromptWindow
{
    public string DisplayText { get; set; } = string.Empty;
    public string OutputText { get; set; } = string.Empty;
    public string PrefilledText { get; set; } = string.Empty;

    public TextPromptWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        
        // Set the prompt text
        PromptLabel.Content = DisplayText;
        
        // Set the prefilled text if provided
        if (!string.IsNullOrEmpty(PrefilledText))
        {
            InputTextBox.Text = PrefilledText;
            InputTextBox.SelectAll();
        }
        
        // Focus on the text box
        InputTextBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
        {
            OutputText = InputTextBox.Text;
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please enter some text.", "Input Required", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}