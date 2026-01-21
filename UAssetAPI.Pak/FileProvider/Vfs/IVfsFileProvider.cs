using System.Diagnostics.CodeAnalysis;
using UAssetAPI.Pak.Encryption.Aes;
using UAssetAPI.Pak.FileProvider.Objects;
using UAssetAPI.Pak.Objects.Core.Misc;
using UAssetAPI.Pak.Readers;
using UAssetAPI.Pak.VirtualFileSystem;

namespace UAssetAPI.Pak.FileProvider.Vfs;
public interface IVfsFileProvider : IFileProvider
{
    public IReadOnlyCollection<IAesVfsReader> UnloadedVfs { get; }
    public IReadOnlyCollection<IAesVfsReader> MountedVfs { get; }

    //Aes-Key Management
    public IReadOnlyDictionary<FGuid, FAesKey> Keys { get; }
    public IReadOnlyCollection<FGuid> RequiredKeys { get; }

    /// <inheritdoc cref="IAesVfsReader.CustomEncryption"/>
    public IAesVfsReader.CustomEncryptionDelegate? CustomEncryption { get; set; }
    public event EventHandler<int>? VfsRegistered;
    public event EventHandler<int>? VfsMounted;
    public event EventHandler<int>? VfsUnmounted;

    /// <summary>
    /// Scan the given directory for archives to register
    /// </summary>
    public void Initialize();

    public void RegisterVfs(string file);
    public void RegisterVfs(FileInfo file);
    public void RegisterVfs(string file, Stream[] stream, Func<string, FArchive>? openContainerStreamFunc = null);

    public int Mount();
    public Task<int> MountAsync();
    public int SubmitKey(FGuid guid, FAesKey key);
    public Task<int> SubmitKeyAsync(FGuid guid, FAesKey key);
    public int SubmitKeys(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys);
    public Task<int> SubmitKeysAsync(IEnumerable<KeyValuePair<FGuid, FAesKey>> keys);

    public IAesVfsReader GetArchive(string archiveName, StringComparison comparison = StringComparison.OrdinalIgnoreCase);
    public bool TryGetArchive(string archiveName, [MaybeNullWhen(false)] out IAesVfsReader archive, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

    public GameFile this[string path, string archiveName, StringComparison comparison = StringComparison.OrdinalIgnoreCase] { get; }
    public GameFile this[string path, IAesVfsReader archive] { get; }

    public bool TryGetGameFile(string path, string archiveName, [MaybeNullWhen(false)] out GameFile file, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

    public byte[] SaveAsset(string path, string archiveName, StringComparison comparison = StringComparison.OrdinalIgnoreCase);
    public byte[] SaveAsset(string path, IAesVfsReader archive);

    public FArchive CreateReader(string path, string archiveName, StringComparison comparison = StringComparison.OrdinalIgnoreCase);
    public FArchive CreateReader(string path, IAesVfsReader archive);

    /// <summary>
    /// Loads and parses a Package from the passed archive.
    /// Can throw various exceptions
    /// </summary>
    /// <param name="path">The package file path</param>
    /// <param name="archiveName">The archive to read from</param>
    /// <param name="comparison">The comparison to use for finding the archive</param>
    /// <returns>The parsed package content</returns>
    public UAsset LoadPackage(string path, string archiveName, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Loads and parses a Package from the passed archive.
    /// Can throw various exceptions
    /// </summary>
    /// <param name="path">The package file path</param>
    /// <param name="archive">The archive to read from</param>
    /// <returns>The parsed package content</returns>
    public UAsset LoadPackage(string path, IAesVfsReader archive);

    public IReadOnlyDictionary<string, byte[]> SavePackage(string path, string archiveName, StringComparison comparison = StringComparison.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, byte[]> SavePackage(string path, IAesVfsReader archive);

    public bool TrySavePackage(string path, string archiveName, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data, StringComparison comparison = StringComparison.OrdinalIgnoreCase);
}
