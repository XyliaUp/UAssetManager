using System.Security.Cryptography;
using UAssetManager.Pak;
using UAssetManager.Pak.Readers;
using UAssetManager.Utils;

namespace UAssetManager.Pak.Objects.Core.Misc;
public readonly struct FSHAHash : IUStruct
{
    public const int SIZE = 20;

    public readonly byte[] Hash;

    public FSHAHash(FArchive Ar)
    {
        Hash = Ar.ReadBytes(SIZE);
    }

    public FSHAHash(FArchive Ar, int customSize)
    {
        Hash = Ar.ReadBytes(customSize);
    }

    public FSHAHash(byte[] data)
    {
        Hash = SHA1.HashData(data);
    }

    public override string ToString()
    {
        unsafe { fixed (byte* ptr = Hash) { return UnsafePrint.BytesToHex(ptr, (uint) Hash.Length); } }
    }
}