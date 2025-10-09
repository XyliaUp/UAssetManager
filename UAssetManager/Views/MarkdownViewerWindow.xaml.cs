using Markdig;
using System.Windows;
using UAssetManager.Resources;

namespace UAssetManager.Views;
public partial class MarkdownViewerWindow : Window
{
    public string MarkdownContent { get; set; } = string.Empty;

    public MarkdownViewerWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        LoadMarkdownContent();
    }

    private void LoadMarkdownContent()
    {
        try
        {
            if (!string.IsNullOrEmpty(MarkdownContent))
            {
                // Use Markdig to convert Markdown to HTML
                var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                string htmlContent = Markdig.Markdown.ToHtml(MarkdownContent, pipeline);
                
                // Create complete HTML document
                string fullHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            margin: 20px;
            background-color: white;
            color: black;
        }}
        h1, h2, h3, h4, h5, h6 {{
            color: #333;
            margin-top: 20px;
            margin-bottom: 10px;
        }}
        code {{
            background-color: #f4f4f4;
            padding: 2px 4px;
            border-radius: 3px;
            font-family: 'Consolas', 'Monaco', monospace;
        }}
        pre {{
            background-color: #f4f4f4;
            padding: 10px;
            border-radius: 5px;
            overflow-x: auto;
        }}
        blockquote {{
            border-left: 4px solid #ddd;
            margin: 0;
            padding-left: 20px;
            color: #666;
        }}
        a {{
            color: #0066cc;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
        }}
        th, td {{
            border: 1px solid #ddd;
            padding: 8px;
            text-align: left;
        }}
        th {{
            background-color: #f2f2f2;
        }}
    </style>
</head>
<body>
    {htmlContent}
</body>
</html>";
                
                MarkdownWebBrowser.NavigateToString(fullHtml);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            MarkdownWebBrowser.NavigateToString($"<html><body><p>Error loading content</p></body></html>");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
