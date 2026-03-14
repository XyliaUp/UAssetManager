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
				case FloatPropertyData f when child.Name.Value == "minvalue":
					model.MinValue = f.Value;
					break;
				case FloatPropertyData f when child.Name.Value == "maxvalue":
					model.MaxValue = f.Value;
					break;
				case ObjectPropertyData obj when child.Name.Value == "distribution":
					model.DistributionObjectIndex = obj.Value;
					break;
				case StructPropertyData tableStruct when child.Name.Value == "table":
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
				case FloatPropertyData f when child.Name.Value == "minvalue":
					f.Value = MinValue;
					break;
				case FloatPropertyData f when child.Name.Value == "maxvalue":
					f.Value = MaxValue;
					break;
				case ObjectPropertyData obj when child.Name.Value == "distribution":
					obj.Value = DistributionObjectIndex;
					break;
				case StructPropertyData tableStruct when child.Name.Value == "table":
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
				case ArrayPropertyData array when child.Name.Value == "values":
					valuesArray = array;
					break;
				case BytePropertyData b when child.Name.Value == "entrycount":
					entryCount = b;
					break;
				case BytePropertyData b when child.Name.Value == "entrystride":
					entryStride = b;
					break;
				case BytePropertyData b when child.Name.Value == "op":
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
				case ArrayPropertyData array when child.Name.Value == "values":
					valuesArray = array;
					break;
				case BytePropertyData b when child.Name.Value == "entrycount":
					entryCount = b;
					break;
				case BytePropertyData b when child.Name.Value == "entrystride":
					entryStride = b;
					break;
				case BytePropertyData b when child.Name.Value == "op":
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
