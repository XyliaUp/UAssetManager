using System.Runtime.CompilerServices;
using UAssetAPI.Pak.Compression;
using UAssetAPI.Pak.Objects;
using UAssetAPI.Pak.Versions;

namespace UAssetAPI.Pak.FileProvider.Objects;
public class OsGameFile : VersionedGameFile
{
    public readonly FileInfo ActualFile;

    public OsGameFile(DirectoryInfo baseDir, FileInfo info, string mountPoint, VersionContainer versions)
        : base(mountPoint + info.FullName.Substring(baseDir.FullName.Length + 1).Replace('\\', '/'), info.Length, versions)
    {
        ActualFile = info;
    }

    public override bool IsEncrypted => false;
    public override CompressionMethod CompressionMethod => CompressionMethod.None;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override byte[] Read() => File.ReadAllBytes(ActualFile.FullName);

	public override void Write(FPakInfo info, BinaryWriter writer)
	{
		throw new NotImplementedException();
	}

	public override void WriteInfo(FPakInfo info, BinaryWriter writer, bool WriteOffset)
	{
		throw new NotImplementedException();
	}
}
