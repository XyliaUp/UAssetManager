using System.Globalization;
using System.Windows.Controls;
using UAssetManager.Resources;

namespace UAssetManager.Controls.Editors;
internal class BindingValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        try
        {
            // Basic validation: check if value is null or empty
            if (value == null)
            {
                return new ValidationResult(false, StringHelper.Get("Validation.ValueCannotBeNull"));
            }

            // Check if value is a valid string
            if (value is string str && string.IsNullOrWhiteSpace(str))
            {
                return new ValidationResult(false, StringHelper.Get("Validation.ValueCannotBeEmpty"));
            }

            return ValidationResult.ValidResult;
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, StringHelper.Get("Validation.ValidationFailed", ex.Message));
        }
    }
}