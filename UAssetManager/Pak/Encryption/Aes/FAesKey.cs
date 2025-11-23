using UAssetManager.Utils;

namespace UAssetManager.Pak.Encryption.Aes;

public class FAesKey
{
    public readonly byte[] Key;
    public readonly string KeyString;
    public bool IsDefault => Key.All(x => x == 0);

    public FAesKey(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Aes Key must be 32 bytes long");
        Key = key;
        KeyString = "0x" + BitConverter.ToString(key);
    }

    public FAesKey(string keyString)
    {
        if (!keyString.StartsWith("0x"))
            keyString = "0x" + keyString;
        if (keyString.Length != 66)
            throw new ArgumentException("Aes Key must be 32 bytes long");
        KeyString = keyString;
        Key = keyString.HexToBytes();
    }

    public override string ToString() => KeyString;
}
