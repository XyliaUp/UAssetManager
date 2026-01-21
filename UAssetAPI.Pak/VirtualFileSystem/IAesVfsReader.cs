using UAssetAPI.Pak.Encryption.Aes;
using UAssetAPI.Pak.Objects.Core.Misc;

namespace UAssetAPI.Pak.VirtualFileSystem;
public interface IAesVfsReader : IVfsReader
{
    public FGuid EncryptionKeyGuid { get; }
    public long Length { get; set; }

    /// <summary>
    /// Custom encryption delegate for AES decryption
    /// It is automatically set based on the game version
    /// But can be overridden if needed
    /// </summary>
    public CustomEncryptionDelegate? CustomEncryption { get; set; }
    public FAesKey? AesKey { get; set; }

    public bool IsEncrypted { get; }
    public int EncryptedFileCount { get; }
    public bool TestAesKey(FAesKey key);
    public byte[] MountPointCheckBytes();

    public void MountTo(object files, StringComparer pathComparer, FAesKey? key, EventHandler<int>? vfsMounted = null);

    public delegate byte[] CustomEncryptionDelegate(byte[] bytes, int beginOffset, int count, bool isIndex, IAesVfsReader reader);
}
