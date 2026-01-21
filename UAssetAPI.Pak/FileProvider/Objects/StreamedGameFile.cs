using UAssetAPI.Pak.Compression;
using UAssetAPI.Pak.Objects;
using UAssetAPI.Pak.Versions;

namespace UAssetAPI.Pak.FileProvider.Objects;
public class StreamedGameFile : VersionedGameFile
{
    private readonly Stream _baseStream;
    private readonly long _position;

    public StreamedGameFile(string path, Stream stream, VersionContainer versions) : base(path, stream.Length, versions)
    {
        _baseStream = stream;
        _position = _baseStream.Position;
    }

    public override bool IsEncrypted => false;
    public override CompressionMethod CompressionMethod => CompressionMethod.None;

    public override byte[] Read()
    {
        var data = new byte[Size];
        var _ = _baseStream.Seek(_position, SeekOrigin.Begin);
        var bytesRead = _baseStream.Read(data, 0, data.Length);
        if (bytesRead != Size)
            throw new FileLoadException("Read operation mismatch: bytesRead ≠ Size");
        return data;
    }

	public override void Write(FPakInfo info, BinaryWriter writer)
	{
		throw new NotImplementedException();
	}

	public override void WriteInfo(FPakInfo info, BinaryWriter writer, bool WriteOffset)
	{
		throw new NotImplementedException();
	}
}