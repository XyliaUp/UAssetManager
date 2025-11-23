using CommunityToolkit.HighPerformance;
using System.Runtime.InteropServices;
using UAssetAPI;
using UAssetAPI.UnrealTypes;
using UAssetManager.Pak.Objects.Core.Misc;
using UAssetManager.Utils;

namespace UAssetManager.Pak.Objects.Engine;
public class ULevel : UObject
{
    //public FPackageIndex?[] Actors { get; private set; }
    //public FURL URL { get; private set; }
    //public FPackageIndex Model { get; private set; }
    //public FPackageIndex[] ModelComponents { get; private set; }
    //public FPackageIndex LevelScriptActor { get; private set; }
    //public FPackageIndex NavListStart { get; private set; }
    //public FPackageIndex NavListEnd { get; private set; }
    //public FPrecomputedVisibilityHandler PrecomputedVisibilityHandler { get; private set; }
    //public FPrecomputedVolumeDistanceField PrecomputedVolumeDistanceField { get; private set; }

    //public override void Deserialize(AssetBinaryReader Ar)
    //{
    //    base.Deserialize(Ar);
    //    Actors = Ar.ReadArray(() => new FPackageIndex(Ar));
    //    URL = new FURL(Ar);
    //    Model = new FPackageIndex(Ar);
    //    ModelComponents = Ar.ReadArray(() => new FPackageIndex(Ar));
    //    LevelScriptActor = new FPackageIndex(Ar);
    //    NavListStart = new FPackageIndex(Ar);
    //    NavListEnd = new FPackageIndex(Ar);
    //    PrecomputedVisibilityHandler = new FPrecomputedVisibilityHandler(Ar);
    //    PrecomputedVolumeDistanceField = new FPrecomputedVolumeDistanceField(Ar);
    //}
}

public class FURL
{
    public FString Protocol;
    public FString Host;
    public int Port;
    public bool Valid;
    public FString Map;
    public FString[] Op;
    public FString Portal;

    public FURL(AssetBinaryReader Ar)
    {
        Protocol = Ar.ReadFString();
        Host = Ar.ReadFString();
        Map = Ar.ReadFString();
        Portal = Ar.ReadFString();
        Op = Ar.ReadArray(Ar.ReadFString);
        Port = Ar.Read<int>();
        Valid = Ar.ReadBoolean();
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPrecomputedVisibilityCell : IUStruct
{
    public readonly FVector Min;
    public readonly ushort ChunkIndex;
    public readonly ushort DataOffset;

    public FPrecomputedVisibilityCell(AssetBinaryReader Ar)
    {
        Min = new FVector(Ar);
        ChunkIndex = Ar.Read<ushort>();
        DataOffset = Ar.Read<ushort>();
    }
}

public readonly struct FCompressedVisibilityChunk : IUStruct
{
    public readonly bool bCompressed;
    public readonly int UncompressedSize;
    public readonly byte[] Data;

    public FCompressedVisibilityChunk(AssetBinaryReader Ar)
    {
        bCompressed = Ar.ReadBoolean();
        UncompressedSize = Ar.Read<int>();
        Data = Ar.ReadBytes(Ar.Read<int>());
    }
}

public readonly struct FPrecomputedVisibilityBucket : IUStruct
{
    public readonly int CellDataSize;
    public readonly FPrecomputedVisibilityCell[] Cells;
    public readonly FCompressedVisibilityChunk[] CellDataChunks;

    public FPrecomputedVisibilityBucket(AssetBinaryReader Ar)
    {
        CellDataSize = Ar.Read<int>();
        Cells = Ar.ReadArray(() => new FPrecomputedVisibilityCell(Ar));
        CellDataChunks = Ar.ReadArray(() => new FCompressedVisibilityChunk(Ar));
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct FPrecomputedVisibilityHandler : IUStruct
{
    public readonly FVector2D PrecomputedVisibilityCellBucketOriginXY;
    public readonly float PrecomputedVisibilityCellSizeXY;
    public readonly float PrecomputedVisibilityCellSizeZ;
    public readonly int PrecomputedVisibilityCellBucketSizeXY;
    public readonly int PrecomputedVisibilityNumCellBuckets;
    public readonly FPrecomputedVisibilityBucket[] PrecomputedVisibilityCellBuckets;

    public FPrecomputedVisibilityHandler(AssetBinaryReader Ar)
    {
        PrecomputedVisibilityCellBucketOriginXY = new FVector2D(Ar);
        PrecomputedVisibilityCellSizeXY = Ar.Read<float>();
        PrecomputedVisibilityCellSizeZ = Ar.Read<float>();
        PrecomputedVisibilityCellBucketSizeXY = Ar.Read<int>();
        PrecomputedVisibilityNumCellBuckets = Ar.Read<int>();
        PrecomputedVisibilityCellBuckets = Ar.ReadArray(() => new FPrecomputedVisibilityBucket(Ar));
    }
}

public readonly struct FPrecomputedVolumeDistanceField : IUStruct
{
    public readonly float VolumeMaxDistance;
    public readonly TBox<float> VolumeBox;
    public readonly int VolumeSizeX;
    public readonly int VolumeSizeY;
    public readonly int VolumeSizeZ;
    public readonly FColor[] Data;

    public FPrecomputedVolumeDistanceField(AssetBinaryReader Ar)
    {
        VolumeMaxDistance = Ar.Read<float>();
        VolumeBox = new TBox<float>(Ar, () => Ar.Read<float>());
        VolumeSizeX = Ar.Read<int>();
        VolumeSizeY = Ar.Read<int>();
        VolumeSizeZ = Ar.Read<int>();
        Data = Ar.ReadArray<FColor>();
    }
}
