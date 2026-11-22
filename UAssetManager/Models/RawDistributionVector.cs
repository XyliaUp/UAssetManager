using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Models;
public class RawDistributionVector
{
	public float MinValue { get; set; }
	public float MaxValue { get; set; }
	public FVector MinValueVec { get; set; }
	public FVector MaxValueVec { get; set; }

	public FPackageIndex DistributionObjectIndex { get; set; }
	public byte Op { get; set; }
	public byte EntryCount { get; set; }
	public byte EntryStride { get; set; }
	public byte SubEntryStride { get; set; }
	public List<float> TableValues { get; set; } = new List<float>();

	// FVector

	public static RawDistributionVector? FromProperty(StructPropertyData? raw)
	{
		if (raw == null) return null;
		var model = new RawDistributionVector();

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
				case StructPropertyData vecStruct when string.Equals(child.Name.Value?.Value, "minvaluevec", StringComparison.OrdinalIgnoreCase):
					model.MinValueVec = ReadVector(vecStruct);
					break;
				case StructPropertyData vecStruct when string.Equals(child.Name.Value?.Value, "maxvaluevec", StringComparison.OrdinalIgnoreCase):
					model.MaxValueVec = ReadVector(vecStruct);
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
				case StructPropertyData vecStruct when string.Equals(child.Name.Value?.Value, "minvaluevec", StringComparison.OrdinalIgnoreCase):
					WriteVector(vecStruct, MinValueVec);
					break;
				case StructPropertyData vecStruct when string.Equals(child.Name.Value?.Value, "maxvaluevec", StringComparison.OrdinalIgnoreCase):
					WriteVector(vecStruct, MaxValueVec);
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

	private static FVector ReadVector(StructPropertyData vecStruct)
	{
		// Expected layout:
		// minvaluevec/maxvaluevec : Struct(vector)
		//   - minvaluevec/maxvaluevec : VectorPropertyData
		var vectorProp = vecStruct.Value.OfType<VectorPropertyData>().FirstOrDefault();
		if (vectorProp?.Value is FVector f) return f;

		return new FVector(0, 0, 0);
	}

	private static void WriteVector(StructPropertyData vecStruct, FVector value)
	{
		var vectorProp = vecStruct.Value.OfType<VectorPropertyData>().FirstOrDefault();
		if (vectorProp == null) return;

		vectorProp.Value = value;
	}

	private void ReadLookupTable(StructPropertyData tableStruct)
	{
		var valuesArray = tableStruct["Values"] as ArrayPropertyData;
		var op = tableStruct["Op"] as BytePropertyData;
		var entryCount = tableStruct["EntryCount"] as BytePropertyData;
		var entryStride = tableStruct["EntryStride"] as BytePropertyData;
		var subEntryStride = tableStruct["SubEntryStride"] as BytePropertyData;

		if (valuesArray == null || entryCount == null || entryStride == null)
			return;

		Op = op?.Value ?? 0;
		EntryCount = entryCount?.Value ?? 0;
		EntryStride = entryStride?.Value ?? 0;
		SubEntryStride = subEntryStride?.Value ?? 0;

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
		var valuesArray = tableStruct["Values"] as ArrayPropertyData;
		var op = tableStruct["Op"] as BytePropertyData;
		var entryCount = tableStruct["EntryCount"] as BytePropertyData;
		var entryStride = tableStruct["EntryStride"] as BytePropertyData;
		var subEntryStride = tableStruct["SubEntryStride"] as BytePropertyData;

		if (valuesArray == null)
			return;

		op!.Value = Op;
		if (entryCount != null) entryCount.Value = EntryCount;
		if (entryStride != null) entryStride.Value = EntryStride;
		if (subEntryStride != null) subEntryStride.Value = SubEntryStride;

		var list = new List<PropertyData>(TableValues.Count);
		for (int i = 0; i < TableValues.Count; i++)
		{
			var fp = new FloatPropertyData() { Value = TableValues[i] };
			list.Add(fp);
		}

		valuesArray.Value = list.ToArray();
	}
}