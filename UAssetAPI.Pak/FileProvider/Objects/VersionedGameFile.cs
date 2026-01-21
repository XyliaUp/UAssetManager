using System.Runtime.CompilerServices;
using UAssetAPI.Pak.Readers;
using UAssetAPI.Pak.Versions;

namespace UAssetAPI.Pak.FileProvider.Objects;
public abstract class VersionedGameFile : GameFile
{
    public readonly VersionContainer Versions;

    public VersionedGameFile(string path, long size, VersionContainer versions) : base(path, size)
    {
        Versions = versions;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override FArchive CreateReader() => new FByteArchive(Path, Read(), Versions);
}
