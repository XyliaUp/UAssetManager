using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using UAssetAPI;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls.Editors;
internal class SoftObjectPropertyEditor(UAsset asset) : PropertyEditorBase, IValueConverter
{
    public override FrameworkElement CreateElement(PropertyData property) => new TextBox
    {

    };

    public override IValueConverter GetConverter() => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is FSoftObjectPath path)
        {
            var packageName = path.AssetPath.PackageName?.ToString() ?? "";
            var assetName = path.AssetPath.AssetName?.ToString() ?? "";
            var subPathString = path.SubPathString?.ToString() ?? "";
            
            var assetPath = !string.IsNullOrEmpty(packageName) && !string.IsNullOrEmpty(assetName)
                ? $"/{packageName}/{assetName}.{assetName}"
                : assetName;
            
            return !string.IsNullOrEmpty(subPathString) ? $"{assetPath}:{subPathString}" : assetPath;
        }

        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string str || string.IsNullOrWhiteSpace(str))
        {
            return new FSoftObjectPath(null, FName.DefineDummy(asset, ""), new FString(""));
        }

        try
        {
            str = str.Trim();
            
            string subPathString = "";
            string assetPath = str;
            
            var colonIndex = str.IndexOf(':');
            if (colonIndex != -1)
            {
                assetPath = str.Substring(0, colonIndex);
                subPathString = str.Substring(colonIndex + 1);
            }
            
            string packageName = "";
            string assetName = "";
            
            if (assetPath.StartsWith('/'))
            {
                var pathParts = assetPath.Substring(1).Split('/');
                if (pathParts.Length >= 2)
                {
                    packageName = string.Join("/", pathParts.Take(pathParts.Length - 1));
                    assetName = pathParts[pathParts.Length - 1];
                }
                else if (pathParts.Length == 1)
                {
                    assetName = pathParts[0];
                }
            }
            else
            {
                assetName = assetPath;
            }
            
            if (assetName.Contains('.'))
            {
                var dotIndex = assetName.LastIndexOf('.');
                assetName = assetName.Substring(0, dotIndex);
            }
            
            var packageNameFName = string.IsNullOrWhiteSpace(packageName) ? null : FName.FromString(asset, packageName);
            var assetNameFName = string.IsNullOrWhiteSpace(assetName) ? FName.DefineDummy(asset, "") : FName.FromString(asset, assetName);
            var subPathFString = string.IsNullOrWhiteSpace(subPathString) ? new FString("") : new FString(subPathString);
            
            return new FSoftObjectPath(packageNameFName, assetNameFName, subPathFString);
        }
        catch (Exception)
        {
            return new FSoftObjectPath(null, FName.DefineDummy(asset, ""), new FString(""));
        }
    }
}