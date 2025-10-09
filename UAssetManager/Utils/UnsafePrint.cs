namespace UAssetManager.Utils;
public static class UnsafePrint
{
    public static unsafe string BytesToHex(byte* bytes, uint length)
    {
        var c = new char[length * 2];

        for (int bx = 0, cx = 0; bx < length; ++bx, ++cx)
        {
            var b = (byte) (bytes[bx] >> 4);
            c[cx] = (char) (b > 9 ? b - 10 + 'A' : b + '0');

            b = (byte) (bytes[bx] & 0x0F);
            c[++cx] = (char) (b > 9 ? b - 10 + 'A' : b + '0');
        }

        return new string(c);
    }

    public static string BytesToHex(params byte[] bytes)
    {
        var hex = BitConverter.ToString(bytes);
        return hex.Replace("-", "");
    }

    public static byte[] HexToBytes(this string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<byte>();
        if (text.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
        {
            var b64 = text.Substring(7).Trim();
            return Convert.FromBase64String(b64);
        }
        
        // hex parsing: allow spaces, tabs, hyphens, 0x prefixes
        var cleaned = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
        cleaned = cleaned.Replace("-", " ");
        var parts = cleaned.Split([' '], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? p.Substring(2) : p)
            .ToArray();
        
        // if no spaces provided, treat as continuous hex string
        if (parts.Length == 1)
        {
            var s = parts[0];
            if (s.Length % 2 != 0) throw new FormatException("Hex length must be even");
            var buf = new byte[s.Length / 2];
            for (int i = 0; i < buf.Length; i++) buf[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return buf;
        }
        else
        {
            var buf = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++) buf[i] = Convert.ToByte(parts[i], 16);
            return buf;
        }
    }
}