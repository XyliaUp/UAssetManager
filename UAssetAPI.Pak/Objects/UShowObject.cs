using UAssetAPI.UnrealTypes;

namespace UAssetAPI.Pak.Objects;

internal class UShowObject : UObject
{
	public FPackageIndex[]? EventKeys;
}

internal abstract class UShowKey : UObject
{

}

internal class UShowParticleKey : UShowKey
{

}