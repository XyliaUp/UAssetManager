using UAssetAPI.Pak.Encryption.Aes;

namespace UAssetManager.Pak.VirtualFileSystem
{
    public abstract partial class AbstractVfsReader
    {
        public void MountTo(object files, StringComparer pathComparer, EventHandler<int>? vfsMounted = null)
        {
            Mount(pathComparer);
            if (files is IDictionary<string, object> dict)
            {
                foreach (var kv in Files)
                {
                    dict[kv.Key] = kv.Value;
                }
                vfsMounted?.Invoke(this, dict.Count);
            }
            else
            {
                vfsMounted?.Invoke(this, FileCount);
            }
        }
    }

    public abstract partial class AbstractAesVfsReader
    {
        public void MountTo(object files, StringComparer pathComparer, FAesKey? key, EventHandler<int>? vfsMounted = null)
        {
            AesKey = key;
            this.MountTo(files, pathComparer, vfsMounted);
        }
    }
}