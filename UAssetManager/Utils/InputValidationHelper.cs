using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using UAssetManager.Resources;

namespace UAssetManager.Utils;
/// <summary>
/// Input validation helper with static methods to validate TextBox input.
/// </summary>
public static class InputValidationHelper
{
    /// <summary>
    /// Setup input validation for a TextBox.
    /// </summary>
    public static TextBox SetupValidation(this TextBox textBox, ValidationType validationType, string? customPattern = null)
    {
        textBox.PreviewTextInput += (s, e) => e.Handled = !IsValidInput(e.Text, textBox.Text, validationType, customPattern);
        textBox.PreviewKeyDown += (s, e) => HandleKeyDown(e, textBox, validationType);
        textBox.TextChanged += (s, e) => UpdateValidationVisual(textBox, validationType, customPattern);
        
        // Initial validation
        UpdateValidationVisual(textBox, validationType, customPattern);
        return textBox;
    }

    /// <summary>
    /// Validate if new input is valid given the current text and validation type.
    /// </summary>
    public static bool IsValidInput(string newText, string currentText, ValidationType validationType, string? customPattern = null)
    {
        if (string.IsNullOrEmpty(newText)) return true;

        // Allow control characters (backspace, delete, etc.)
        if (newText.Length == 1 && char.IsControl(newText[0])) return true;

        var testText = currentText + newText;

        return validationType switch
        {
            ValidationType.Byte => IsValidByte(testText),
            ValidationType.Integer => IsValidInteger(testText),
            ValidationType.Float => IsValidFloat(testText),
            ValidationType.PackageIndex => IsValidPackageIndex(testText),
            ValidationType.Url => IsValidUrl(testText),
            ValidationType.Regex => IsValidRegex(testText, customPattern),
            _ => true
        };
    }

    /// <summary>
    /// Handle key down events to allow typical editing/navigation keys.
    /// </summary>
    private static void HandleKeyDown(KeyEventArgs e, TextBox textBox, ValidationType validationType)
    {
        // Allow control keys (Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+Z, etc.) and Backspace/Delete
        if (e.Key == Key.Back || e.Key == Key.Delete || 
            (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            return;
        }

        // Allow numeric keys
        if ((e.Key >= Key.D0 && e.Key <= Key.D9) || 
            (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
        {
            return;
        }

        // Allow minus sign only at start (for signed numbers)
        if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
        {
            if (textBox.SelectionStart == 0 && !textBox.Text.StartsWith("-"))
            {
                return;
            }
        }

        // Allow decimal point for floating numbers
        if (validationType == ValidationType.Float && 
            (e.Key == Key.Decimal || e.Key == Key.OemPeriod))
        {
            if (!textBox.Text.Contains("."))
            {
                return;
            }
        }

        e.Handled = true;
    }

    private static bool IsValidInteger(string text)
    {
        return string.IsNullOrEmpty(text) || 
               text == "-" || 
               int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsValidByte(string text)
    {
        return string.IsNullOrEmpty(text) ||
               byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsValidFloat(string text)
    {
        return string.IsNullOrEmpty(text) || 
               text == "-" || 
               text == "." || 
               text == "-." ||
               text.EndsWith("e") || text.EndsWith("E") ||
               text.EndsWith("e-") || text.EndsWith("E-") ||
               text.EndsWith("e+") || text.EndsWith("E+") ||
               float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private static bool IsValidPackageIndex(string text)
    {
        return string.IsNullOrEmpty(text) || 
               text == "-" || 
               int.TryParse(text, out _);
    }

    private static bool IsValidUrl(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        
        try
        {
            var urlRegex = new System.Text.RegularExpressions.Regex(@"^https?://[^\s/$.?#].[^\s]*$");
            return urlRegex.IsMatch(text);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidRegex(string text, string? pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern)) return true;
        
        try
        {
            var regex = new System.Text.RegularExpressions.Regex(pattern);
            return regex.IsMatch(text);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Update validation visual feedback.
    /// </summary>
    private static void UpdateValidationVisual(TextBox textBox, ValidationType validationType, string? customPattern = null)
    {
        if (validationType == ValidationType.None) return;
        
        var isValid = IsValidInput("", textBox.Text, validationType, customPattern);
        
        // Update border color
        textBox.BorderBrush = isValid ? System.Windows.Media.Brushes.Gray : System.Windows.Media.Brushes.Red;
        textBox.BorderThickness = new System.Windows.Thickness(isValid ? 1 : 2);
        
        // Update tooltip
        if (!isValid && !string.IsNullOrEmpty(textBox.Text))
        {
            textBox.ToolTip = GetValidationErrorMessage(validationType);
        }
        else if (isValid)
        {
            textBox.ToolTip = null;
        }
    }

    /// <summary>
    /// Get validation error message
    /// </summary>
    private static string GetValidationErrorMessage(ValidationType validationType)
    {
        return validationType switch
        {
            ValidationType.Byte => StringHelper.Get("Validation.Byte"),
            ValidationType.Integer => StringHelper.Get("Validation.Integer"),
            ValidationType.Float => StringHelper.Get("Validation.Float"),
            ValidationType.PackageIndex => StringHelper.Get("Validation.PackageIndex"),
            ValidationType.Url => StringHelper.Get("Validation.Url"),
            ValidationType.Regex => StringHelper.Get("Validation.Regex"),
            _ => StringHelper.Get("Validation.Regex")
        };
    }
}

/// <summary>
/// Validation type enumeration
/// </summary>
public enum ValidationType
{
    None,
    Byte,
    Integer,
    Float,
    PackageIndex,
    Url,
    Regex,
    Custom
}