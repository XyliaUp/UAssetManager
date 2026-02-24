using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using UAssetAPI.Pak.Pak.Utils;
using AesProvider = System.Security.Cryptography.Aes;

namespace UAssetAPI.Pak.Encryption.Aes;

public static class Aes
{
    public const int ALIGN = 16;
    public const int BLOCK_SIZE = 16 * 8;

    private static readonly AesProvider Provider;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(this byte[] encrypted, FAesKey key)
    {
        return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, 0, encrypted.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decrypt(this byte[] encrypted, int beginOffset, int count, FAesKey key)
    {
        return Provider.CreateDecryptor(key.Key, null).TransformFinalBlock(encrypted, beginOffset, count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Encrypt(this byte[] decrypted, FAesKey key)
    {
        return Provider.CreateEncryptor(key.Key, null).TransformFinalBlock(decrypted, 0, decrypted.Length);
    }

    public static void Align(ref byte[] bytes)
    {
        var size = bytes.Length.Align(ALIGN);
        if (size != bytes.Length)
        {
            var index = 0;
            var temp = new byte[size];
            while (index < size)
            {
                Buffer.BlockCopy(bytes, 0, temp, index, Math.Min(bytes.Length, size - index));
                index += bytes.Length;
            }

            bytes = temp;
        }
    }

    static Aes()
    {
        Provider = AesProvider.Create();
        Provider.Mode = CipherMode.ECB;
        Provider.Padding = PaddingMode.None;
        Provider.BlockSize = BLOCK_SIZE;
    }
}