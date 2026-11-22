using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Models;
internal class RawDistributionFloat
{
	public float MinValue { get; set; }
	public float MaxValue { get; set; }

	public FPackageIndex DistributionObjectIndex { get; set; }

	public byte Op { get; set; }
	public byte EntryCount { get; set; }
	public byte EntryStride { get; set; }

	public List<float> TableValues { get; set; } = new List<float>();

	public static RawDistributionFloat? FromProperty(StructPropertyData? raw)
	{
		if (raw == null) return null;
		var model = new RawDistributionFloat();

		foreach (var child in raw.Value)
		{
			switch (child)
			{
				case FloatPropertyData f when string.Equals(child.Name.Value?.Value, "minvalue", StringComparison.OrdinalIgnoreCase):
					model.MinValue = f.Value;
					break;
				case FloatPropertyData f when string.Equals(child.Name.Value?.Value, "maxvalue", StringComparison.OrdinalIgnoreCase):
					model.MaxValue = f.Value;
					break;
				case ObjectPropertyData obj when string.Equals(child.Name.Value?.Value, "distribution", StringComparison.OrdinalIgnoreCase):
					model.DistributionObjectIndex = obj.Value;
					break;
				case StructPropertyData tableStruct when string.Equals(child.Name.Value?.Value, "table", StringComparison.OrdinalIgnoreCase):
					model.ReadLookupTable(tableStruct);
					break;
			}
		}

		return model;
	}

	public void ApplyToProperty(StructPropertyData raw)
	{
		foreach (var child in raw.Value)
		{
			switch (child)
			{
				case FloatPropertyData f when string.Equals(child.Name.Value?.Value, "minvalue", StringComparison.OrdinalIgnoreCase):
					f.Value = MinValue;
					break;
				case FloatPropertyData f when string.Equals(child.Name.Value?.Value, "maxvalue", StringComparison.OrdinalIgnoreCase):
					f.Value = MaxValue;
					break;
				case ObjectPropertyData obj when string.Equals(child.Name.Value?.Value, "distribution", StringComparison.OrdinalIgnoreCase):
					obj.Value = DistributionObjectIndex;
					break;
				case StructPropertyData tableStruct when string.Equals(child.Name.Value?.Value, "table", StringComparison.OrdinalIgnoreCase):
					WriteLookupTable(tableStruct);
					break;
			}
		}
	}

	private void ReadLookupTable(StructPropertyData tableStruct)
	{
		ArrayPropertyData? valuesArray = null;
		BytePropertyData? entryCount = null;
		BytePropertyData? entryStride = null;
		BytePropertyData? op = null;

		foreach (var child in tableStruct.Value)
		{
			switch (child)
			{
				case ArrayPropertyData array when string.Equals(child.Name.Value?.Value, "values", StringComparison.OrdinalIgnoreCase):
					valuesArray = array;
					break;
				case BytePropertyData b when string.Equals(child.Name.Value?.Value, "entrycount", StringComparison.OrdinalIgnoreCase):
					entryCount = b;
					break;
				case BytePropertyData b when string.Equals(child.Name.Value?.Value, "entrystride", StringComparison.OrdinalIgnoreCase):
					entryStride = b;
					break;
				case BytePropertyData b when string.Equals(child.Name.Value?.Value, "op", StringComparison.OrdinalIgnoreCase):
					op = b;
					break;
			}
		}

		if (valuesArray == null || entryCount == null || entryStride == null)
			return;

		Op = op?.Value ?? 0;
		EntryCount = entryCount.Value;
		EntryStride = entryStride.Value;

		TableValues.Clear();
		foreach (var element in valuesArray.Value)
		{
			if (element is FloatPropertyData fp)
			{
				TableValues.Add(fp.Value);
			}
		}
	}

	private void WriteLookupTable(StructPropertyData tableStruct)
	{
		ArrayPropertyData? valuesArray = null;
		BytePropertyData? entryCount = null;
		BytePropertyData? entryStride = null;
		BytePropertyData? op = null;

		foreach (var child in tableStruct.Value)
		{
			switch (child)
			{
				case ArrayPropertyData array when string.Equals(child.Name.Value?.Value, "values", StringComparison.OrdinalIgnoreCase):
					valuesArray = array;
					break;
				case BytePropertyData b when string.Equals(child.Name.Value?.Value, "entrycount", StringComparison.OrdinalIgnoreCase):
					entryCount = b;
					break;
				case BytePropertyData b when string.Equals(child.Name.Value?.Value, "entrystride", StringComparison.OrdinalIgnoreCase):
					entryStride = b;
					break;
				case BytePropertyData b when string.Equals(child.Name.Value?.Value, "op", StringComparison.OrdinalIgnoreCase):
					op = b;
					break;
			}
		}

		if (valuesArray == null || entryCount == null || entryStride == null)
			return;

		op!.Value = Op;
		entryCount.Value = EntryCount;
		entryStride.Value = EntryStride;

		var list = new List<PropertyData>(TableValues.Count);
		for (int i = 0; i < TableValues.Count; i++)
		{
			var fp = new FloatPropertyData() { Value = TableValues[i] };
			list.Add(fp);
		}

		valuesArray.Value = list.ToArray();
	}
}
