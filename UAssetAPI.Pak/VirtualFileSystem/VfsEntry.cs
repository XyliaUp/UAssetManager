using UAssetAPI.Pak.FileProvider.Objects;

namespace UAssetAPI.Pak.VirtualFileSystem;
public abstract class VfsEntry : GameFile
{
    public IVfsReader Vfs { get; }
    public long Offset { get; set; }

    protected VfsEntry(IVfsReader vfs, string path, long size = 0) : base(path, size)
    {
        Vfs = vfs;
    }

    protected VfsEntry(IVfsReader vfs)
    {
        Vfs = vfs;
    }
}