using OffiUtils;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UAssetAPI;
using UAssetManager.Models;
using UAssetManager.Pak.Compression;
using UAssetManager.Pak.Encryption.Aes;
using UAssetManager.Pak.Exceptions;
using UAssetManager.Pak.Objects;
using UAssetManager.Pak.Objects.Core.Misc;
using UAssetManager.Pak.Pak.Objects;
using UAssetManager.Pak.Readers;
using UAssetManager.Pak.Versions;
using UAssetManager.Pak.VirtualFileSystem;
using UAssetManager.Utils;
using static UAssetManager.Pak.Compression.Compression;
using static UAssetManager.Pak.Pak.Objects.EPakFileVersion;

namespace UAssetManager.Pak;
public partial class PakFileReader : AbstractAesVfsReader
{
    public readonly FArchive Ar;
    public readonly FPakInfo Info;

    public override string MountPoint { get; set; }
    public sealed override long Length { get; set; }

    public override bool HasDirectoryIndex => true;
    public override FGuid EncryptionKeyGuid => Info.EncryptionKeyGuid;
    public override bool IsEncrypted => Info.EncryptedIndex;

    public PakFileReader() : base("", new() { Game = EGame.GAME_BladeAndSoul })
    {
        AesKey = new FAesKey(UAGConfig.Data.AesKey);
        Info = new FPakInfo();
    }

    public PakFileReader(FArchive Ar) : base(Ar.Name, Ar.Versions)
    {
        this.Ar = Ar;
        Length = Ar.Length;
        Info = FPakInfo.ReadFPakInfo(Ar);
        if (Info.Version > PakFile_Version_Latest &&
            Ar.Game != EGame.GAME_TowerOfFantasy && Ar.Game != EGame.GAME_MeetYourMaker &&
            Ar.Game != EGame.GAME_Snowbreak && Ar.Game != EGame.GAME_TheDivisionResurgence &&
            Ar.Game != EGame.GAME_TorchlightInfinite && Ar.Game != EGame.GAME_DeadByDaylight &&
            Ar.Game != EGame.GAME_QQ && Ar.Game != EGame.GAME_DreamStar) // These games use version >= 12 to indicate their custom formats
        {
            Log.Warning($"Pak file \"{Name}\" has unsupported version {(int)Info.Version}");
        }
    }

    public PakFileReader(string filePath, VersionContainer? versions = null)
        : this(new FileInfo(filePath), versions) { }
    public PakFileReader(FileInfo file, VersionContainer? versions = null)
        : this(file.FullName, file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), versions) { }
    public PakFileReader(string filePath, Stream stream, VersionContainer? versions = null)
        : this(new FStreamArchive(filePath, stream, versions)) { }
    public PakFileReader(string filePath, RandomAccessStream stream, VersionContainer? versions = null)
        : this(new FRandomAccessStreamArchive(filePath, stream, versions)) { }

    public override byte[] Extract(VfsEntry entry)
    {
        if (entry is not FPakEntry pakEntry || entry.Vfs != this) throw new ArgumentException($"Wrong pak file reader, required {entry.Vfs.Name}, this is {Name}");
        // If this reader is used as a concurrent reader create a clone of the main reader to provide thread safety
        var reader = IsConcurrent ? (FArchive)Ar.Clone() : Ar;
        if (pakEntry.IsCompressed)
        {
#if DEBUG
            Log.Debug("{EntryName} is compressed with {CompressionMethod}", pakEntry.Name, pakEntry.CompressionMethod);
#endif

            var uncompressed = new byte[(int)pakEntry.UncompressedSize];
            var uncompressedOff = 0;
            foreach (var block in pakEntry.CompressionBlocks)
            {
                var blockSize = (int)block.Size;
                var srcSize = blockSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
                // Read the compressed block
                var compressed = ReadAndDecryptAt(block.CompressedStart, srcSize, reader, pakEntry.IsEncrypted);
                // Calculate the uncompressed size,
                // its either just the compression block size,
                // or if it's the last block, it's the remaining data size
                var uncompressedSize = (int)Math.Min(pakEntry.CompressionBlockSize, pakEntry.UncompressedSize - uncompressedOff);
                Decompress(compressed, 0, blockSize, uncompressed, uncompressedOff, uncompressedSize, pakEntry.CompressionMethod);
                uncompressedOff += (int)pakEntry.CompressionBlockSize;
            }

            return uncompressed;
        }

        // Pak Entry is written before the file data,
        // but it's the same as the one from the index, just without a name
        // We don't need to serialize that again so + file.StructSize
        var size = (int)pakEntry.UncompressedSize.Align(pakEntry.IsEncrypted ? Aes.ALIGN : 1);
        var data = ReadAndDecryptAt(pakEntry.Offset + pakEntry.StructSize /* Doesn't seem to be the case with older pak versions */,
            size, reader, pakEntry.IsEncrypted);
        return size != pakEntry.UncompressedSize ? data.SubByteArray((int)pakEntry.UncompressedSize) : data;
    }

    public override void Mount(StringComparer pathComparer)
    {
        var watch = new Stopwatch();
        watch.Start();

        if (Info.Version >= PakFile_Version_PathHashIndex)
            ReadIndexUpdated(pathComparer);
        else if (Info.IndexIsFrozen)
            ReadFrozenIndex(pathComparer);
        else
            ReadIndexLegacy(pathComparer);

        if (!IsEncrypted && EncryptedFileCount > 0)
        {
            Log.Warning($"Pak file \"{Name}\" is not encrypted but contains encrypted files");
        }

        var elapsed = watch.Elapsed;
        var sb = new StringBuilder($"Pak \"{Name}\": {FileCount} files");
        if (EncryptedFileCount > 0) sb.Append($" ({EncryptedFileCount} encrypted)");
        if (MountPoint.Contains("/")) sb.Append($", mount point: \"{MountPoint}\"");
        sb.Append($", order {ReadOrder}");
        sb.Append($", version {(int)Info.Version} in {elapsed}");
        Log.Information(sb.ToString());
    }

    private void ReadIndexLegacy(StringComparer pathComparer)
    {
        Ar.Position = Info.IndexOffset;
        var index = new FByteArchive($"{Name} - Index", ReadAndDecrypt((int)Info.IndexSize), Versions);

        string mountPoint;
        try
        {
            mountPoint = index.ReadFString();
        }
        catch (Exception e)
        {
            throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}' is not working with '{Name}'", e);
        }

        ValidateMountPoint(ref mountPoint);
        MountPoint = mountPoint;

        var fileCount = index.Read<int>();
        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        for (var i = 0; i < fileCount; i++)
        {
            var path = string.Concat(mountPoint, index.ReadFString());
            var entry = new FPakEntry(this, path, index);
            if (entry is { IsDeleted: true, Size: 0 }) continue;
            if (entry.IsEncrypted) EncryptedFileCount++;
            files[path] = entry;
        }

        Files = files;
    }

    private void ReadIndexUpdated(StringComparer pathComparer)
    {
        // Prepare primary index and decrypt if necessary
        Ar.Position = Info.IndexOffset;
        FArchive primaryIndex = new FByteArchive($"{Name} - Primary Index", ReadAndDecrypt((int)Info.IndexSize));

        int fileCount = 0;
        EncryptedFileCount = 0;

        if (Ar.Game is EGame.GAME_DreamStar or EGame.GAME_DeltaForceHawkOps)
        {
            primaryIndex.Position += 8; // PathHashSeed
            fileCount = primaryIndex.Read<int>();
        }

        string mountPoint;
        try
        {
            mountPoint = primaryIndex.ReadFString();
        }
        catch (Exception e)
        {
            throw new InvalidAesKeyException($"Given aes key '{AesKey?.KeyString}' is not working with '{Name}'", e);
        }

        ValidateMountPoint(ref mountPoint);
        MountPoint = mountPoint;

        if (!(Ar.Game is EGame.GAME_DreamStar or EGame.GAME_DeltaForceHawkOps))
        {
            fileCount = primaryIndex.Read<int>();
            primaryIndex.Position += 8; // PathHashSeed
        }

        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No path hash index");

        primaryIndex.Position += 36; // PathHashIndexOffset (long) + PathHashIndexSize (long) + PathHashIndexHash (20 bytes)
        if (Ar.Game == EGame.GAME_Rennsport) primaryIndex.Position += 16;

        if (!primaryIndex.ReadBoolean())
            throw new ParserException(primaryIndex, "No directory index");

        if (Ar.Game == EGame.GAME_TheDivisionResurgence) primaryIndex.Position += 40; // duplicate entry

        var directoryIndexOffset = primaryIndex.Read<long>();
        var directoryIndexSize = primaryIndex.Read<long>();
        primaryIndex.Position += 20; // Directory Index hash
        if (Ar.Game == EGame.GAME_Rennsport) primaryIndex.Position += 20;
        var encodedPakEntriesSize = primaryIndex.Read<int>();
        if (Ar.Game == EGame.GAME_Rennsport)
        {
            primaryIndex.Position -= 4;
            encodedPakEntriesSize = (int)(primaryIndex.Length - primaryIndex.Position - 6);
        }
        var encodedPakEntries = primaryIndex.ReadBytes(encodedPakEntriesSize);

        if (primaryIndex.Read<int>() < 0)
            throw new ParserException("Corrupt pak PrimaryIndex detected");

        // Read FDirectoryIndex
        Ar.Position = directoryIndexOffset;
        var directoryIndex = new FByteArchive($"{Name} - Directory Index", ReadAndDecrypt((int)directoryIndexSize));
        var directoryIndexLength = directoryIndex.Read<int>();

        var files = new Dictionary<string, GameFile>(fileCount, pathComparer);
        unsafe
        {
            fixed (byte* ptr = encodedPakEntries)
            {
                for (var i = 0; i < directoryIndexLength; i++)
                {
                    var dir = directoryIndex.ReadFString();
                    var dirDictLength = directoryIndex.Read<int>();

                    for (var j = 0; j < dirDictLength; j++)
                    {
                        var name = directoryIndex.ReadFString();
                        string path;
                        if (mountPoint.EndsWith('/') && dir.StartsWith('/'))
                            path = dir.Length == 1 ? string.Concat(mountPoint, name) : string.Concat(mountPoint, dir[1..], name);
                        else
                            path = string.Concat(mountPoint, dir, name);

                        var offset = directoryIndex.Read<int>();
                        if (offset == int.MinValue) continue;

                        var entry = new FPakEntry(this, path, ptr + offset);
                        if (entry.IsEncrypted) EncryptedFileCount++;
                        files[path] = entry;
                    }
                }
            }
        }

        Files = files;
    }

    private void ReadFrozenIndex(StringComparer pathComparer)
    {
        this.Ar.Position = Info.IndexOffset;
        var Ar = new FMemoryImageArchive(new FByteArchive("FPakFileData", this.Ar.ReadBytes((int)Info.IndexSize)), 8);

        var mountPoint = Ar.ReadFString();
        ValidateMountPoint(ref mountPoint);
        MountPoint = mountPoint;

        var entries = Ar.ReadArray(() => new FPakEntry(this, Ar));

        // read TMap<FString, TMap<FString, int32>>
        var index = Ar.ReadTMap(
            () => Ar.ReadFString(),
            () => Ar.ReadTMap(
                () => Ar.ReadFString(),
                () => Ar.Read<int>(),
                16, 4
            ),
            16, 56
        );

        var files = new Dictionary<string, GameFile>(entries.Length, pathComparer);
        foreach (var (dir, dirContents) in index)
        {
            foreach (var (name, fileIndex) in dirContents)
            {
                string path;
                if (mountPoint.EndsWith('/') && dir.StartsWith('/'))
                    path = dir.Length == 1 ? string.Concat(mountPoint, name) : string.Concat(mountPoint, dir[1..], name);
                else
                    path = string.Concat(mountPoint, dir, name);

                var entry = entries[fileIndex];
                entry.Path = path;

                if (entry is { IsDeleted: true, Size: 0 }) continue;
                if (entry.IsEncrypted) EncryptedFileCount++;
                files[path] = entry;
            }
        }

        Files = files;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override byte[] ReadAndDecrypt(int length) => ReadAndDecrypt(length, Ar, IsEncrypted);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] EncryptIfEncrypted(byte[] bytes) => EncryptIfEncrypted(bytes, IsEncrypted);

    public byte[] EncryptIfEncrypted(byte[] bytes, bool isEncrypted)
    {
        if (!isEncrypted) return bytes;
        if (AesKey != null)
        {
            Aes.Align(ref bytes);
            return bytes.Encrypt(AesKey);
        }

        throw new InvalidAesKeyException("Encrypt data requires a valid aes key");
    }

    public override byte[] MountPointCheckBytes()
    {
        var reader = IsConcurrent ? (FArchive)Ar.Clone() : Ar;
        reader.Position = Info.IndexOffset;
        var size = Math.Min((int)Info.IndexSize, 4 + MAX_MOUNTPOINT_TEST_LENGTH * 2);
        return reader.ReadBytes(size.Align(Aes.ALIGN));
    }

    public override void Dispose()
    {
        Ar.Dispose();
    }

    public void AddFile(string filePath, string vfsPath, CompressionMethod method = CompressionMethod.None)
    {
        vfsPath = vfsPath.Replace("\\", "/");
        Files.Add(vfsPath, new FPakEntry(this, File.ReadAllBytes(filePath), vfsPath, method));
    }

    public void AddFile(GameFile gameFile, CompressionMethod method = CompressionMethod.None)
    {
        var vfsPath = gameFile.Path.Replace("\\", "/");
        Files.Add(vfsPath, new FPakEntry(this, gameFile.Read(), vfsPath, method));
    }

    /// <summary>
    /// Generates MountPoint by finding the common path prefix from all file paths in Files.Values
    /// </summary>
    /// <returns>The common path prefix, or "/" if no common path is found</returns>
    private string GenerateMountPointFromFiles()
    {
        if (Files.Count == 0) return "/";

        var paths = Files.Values.Select(f => f.Path.Replace("\\", "/")).ToList();

        // Start with the first path as common prefix
        string commonPrefix = paths[0];

        // Find common prefix with all other paths
        foreach (var path in paths.Skip(1))
        {
            int minLength = Math.Min(commonPrefix.Length, path.Length);
            int i = 0;
            for (; i < minLength; i++)
            {
                if (commonPrefix[i] != path[i]) break;
            }
            commonPrefix = commonPrefix.Substring(0, i);
            if (string.IsNullOrEmpty(commonPrefix)) break;
        }

        // Find the last directory separator to ensure complete directory path
        int lastSlashIndex = commonPrefix.LastIndexOf('/');
        if (lastSlashIndex >= 0) commonPrefix = commonPrefix.Substring(0, lastSlashIndex + 1);
        else commonPrefix = "/";
        return commonPrefix;
    }

    /// <summary>
    /// Writes the current data structure to the specified <see cref="BinaryWriter"/>.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter"/> to which the data will be written. This writer must be open and writable.</param>
    public void Write(BinaryWriter writer)
    {
        #region init
        MountPoint = GenerateMountPointFromFiles();

        // Version.Compression
        if (Info.Version >= PakFile_Version_FNameBasedCompressionMethod)
        {
            Info.CompressionMethods = [CompressionMethod.None,
                .. Files.Select(f => f.Value.CompressionMethod).Where(x => x != 0).Distinct()];
        }
        #endregion

        #region File Data Section
        var files = Files.Where(x => !x.Key.Contains(".placeholder"));
        foreach (var (path, file) in files)
        {
            file.Write(Info, writer);
        }
        #endregion

        #region File Index Section
        Info.IndexOffset = writer.BaseStream.Position;

        using var ms = new MemoryStream();
        var IndexWriter = new UnrealBinaryWriter(ms);
        IndexWriter.Write("../../../" + MountPoint);
        IndexWriter.Write(files.Count());

        foreach (var (path, file) in files)
        {
            // Index stores path relative to mount point; strip leading '/' to avoid double slash after mount point
            var relativePath = file.Path;
            if (relativePath.StartsWith(MountPoint, StringComparison.OrdinalIgnoreCase))
                relativePath = relativePath.Substring(MountPoint.Length);
            relativePath = relativePath.TrimStart('/');
            IndexWriter.Write(relativePath);
            file.WriteInfo(Info, IndexWriter, true);
        }

        byte[] temp = ms.GetBuffer();
        var IndexData = EncryptIfEncrypted(temp);
        writer.Write(IndexData);

        Info.IndexSize = IndexData.Length;
        Info.IndexHash = new FSHAHash(temp);

        IndexData = null;
        #endregion

        Info.Write(writer);

        writer.Flush();
        writer.Close();
        writer.Dispose();
    }
}