using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;

namespace UAssetAPI.Pak.Utils;
public static class PropertyUtils
{
	public static T? Find<T>(this List<PropertyData> properties, string name) where T : PropertyData
		=> properties.FirstOrDefault(p => string.Equals(p.Name.Value?.Value, name, StringComparison.OrdinalIgnoreCase)) as T;

	public static T? Find<T>(this Export export, string name) where T : PropertyData
		=> (export as NormalExport)?.Data.Find<T>(name);
}
