using UAssetAPI.Pak.FileProvider.Objects;
using UAssetAPI.Pak.Versions;

namespace UAssetAPI.Pak.VirtualFileSystem;
public interface IVfsReader : IDisposable
{
    public string Path { get; }
    public string Name { get; }
    public long ReadOrder { get; }

    public Dictionary<string, GameFile> Files { get; }
    public int FileCount { get; }

    public string MountPoint { get; }
    public bool HasDirectoryIndex { get; }
    public bool IsConcurrent { get; set; }

    public VersionContainer Versions { get; set; }
    public EGame Game { get; set; }
    public FPackageFileVersion Ver { get; set; }

    public void Mount(StringComparer pathComparer);
    public void MountTo(object files, StringComparer pathComparer, EventHandler<int>? vfsMounted = null);

    public byte[] Extract(VfsEntry entry);
}
