using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetManager.Utils;

internal static class ExportPropertyLookup
{
    internal static T? FindProperty<T>(this Export? export, string name) where T : PropertyData
    {
        return (export as NormalExport)?.Data.FirstOrDefault(property =>
            string.Equals(property.Name.Value?.Value, name, StringComparison.OrdinalIgnoreCase)) as T;
    }
}
