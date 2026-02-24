using UAssetAPI;
using UAssetAPI.Pak.Objects;
using UAssetAPI.Pak.Pak.Utils;
using UAssetAPI.UnrealTypes;

namespace UAssetAPI.Pak.Objects.Engine;
public class UWorld : UObject
{
    public FPackageIndex PersistentLevel { get; private set; }
    public FPackageIndex[] ExtraReferencedObjects { get; private set; }
    public FPackageIndex[] StreamingLevels { get; private set; }

    public override void Deserialize(AssetBinaryReader Ar)
    {
        base.Deserialize(Ar);
        PersistentLevel = new FPackageIndex(Ar);
        ExtraReferencedObjects = Ar.ReadArray(() => new FPackageIndex(Ar));
        StreamingLevels = Ar.ReadArray(() => new FPackageIndex(Ar));
    }

    public override void Serialize(AssetBinaryWriter Aw)
    {
        base.Serialize(Aw);
        PersistentLevel.Write(Aw);
        Aw.WriteArray(ExtraReferencedObjects, (o) => o.Write(Aw));
        Aw.WriteArray(StreamingLevels, (o) => o.Write(Aw));
    }
}