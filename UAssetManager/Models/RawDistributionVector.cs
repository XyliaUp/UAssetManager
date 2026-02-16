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

	public List<float> TableValues { get; set; } = new List<float>();

	public static RawDistributionVector? FromProperty(StructPropertyData? raw)
	{
		if (raw == null) return null;
		var model = new RawDistributionVector();

		foreach (var child in raw.Value)
		{
			switch (child)
			{
				case FloatPropertyData f when child.Name.ToString() == "minvalue":
					model.MinValue = f.Value;
					break;
				case FloatPropertyData f when child.Name.ToString() == "maxvalue":
					model.MaxValue = f.Value;
					break;
				case StructPropertyData vecStruct when child.Name.ToString() == "minvaluevec":
					model.MinValueVec = ReadVector(vecStruct);
					break;
				case StructPropertyData vecStruct when child.Name.ToString() == "maxvaluevec":
					model.MaxValueVec = ReadVector(vecStruct);
					break;
				case ObjectPropertyData obj when child.Name.ToString() == "distribution":
					model.DistributionObjectIndex = obj.Value;
					break;
				case StructPropertyData tableStruct when child.Name.ToString() == "table":
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
				case FloatPropertyData f when child.Name.ToString() == "minvalue":
					f.Value = MinValue;
					break;
				case FloatPropertyData f when child.Name.ToString() == "maxvalue":
					f.Value = MaxValue;
					break;
				case StructPropertyData vecStruct when child.Name.ToString() == "minvaluevec":
					WriteVector(vecStruct, MinValueVec);
					break;
				case StructPropertyData vecStruct when child.Name.ToString() == "maxvaluevec":
					WriteVector(vecStruct, MaxValueVec);
					break;
				case ObjectPropertyData obj when child.Name.ToString() == "distribution":
					obj.Value = DistributionObjectIndex;
					break;
				case StructPropertyData tableStruct when child.Name.ToString() == "table":
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
		if (vectorProp?.Value is FVector f)
		{
			return f;
		}

		return new FVector(0, 0, 0);
	}

	private static void WriteVector(StructPropertyData vecStruct, FVector value)
	{
		var vectorProp = vecStruct.Value.OfType<VectorPropertyData>().FirstOrDefault();
		if (vectorProp == null)
			return;

		vectorProp.Value = value;
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
				case ArrayPropertyData array when child.Name.ToString() == "values":
					valuesArray = array;
					break;
				case BytePropertyData b when child.Name.ToString() == "entrycount":
					entryCount = b;
					break;
				case BytePropertyData b when child.Name.ToString() == "entrystride":
					entryStride = b;
					break;
				case BytePropertyData b when child.Name.ToString() == "op":
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
				case ArrayPropertyData array when child.Name.ToString() == "values":
					valuesArray = array;
					break;
				case BytePropertyData b when child.Name.ToString() == "entrycount":
					entryCount = b;
					break;
				case BytePropertyData b when child.Name.ToString() == "entrystride":
					entryStride = b;
					break;
				case BytePropertyData b when child.Name.ToString() == "op":
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