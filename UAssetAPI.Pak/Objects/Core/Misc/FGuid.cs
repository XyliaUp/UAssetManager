using System.Runtime.InteropServices;
using UAssetManager.Pak;

namespace UAssetAPI.Pak.Objects.Core.Misc;
public enum EGuidFormats
{
    Digits, // "00000000000000000000000000000000"
    DigitsWithHyphens, // 00000000-0000-0000-0000-000000000000
    DigitsWithHyphensInBraces, // {00000000-0000-0000-0000-000000000000}
    DigitsWithHyphensInParentheses, // (00000000-0000-0000-0000-000000000000)
    HexValuesInBraces, // {0x00000000,0x0000,0x0000,{0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00}}
    UniqueObjectGuid, // 00000000-00000000-00000000-00000000
    Short, // AQsMCQ0PAAUKCgQEBAgADQ
    Base36Encoded, // 1DPF6ARFCM4XH5RMWPU8TGR0J
};

[StructLayout(LayoutKind.Sequential)]
public struct FGuid : IUStruct
{
    public readonly uint A;
    public readonly uint B;
    public readonly uint C;
    public readonly uint D;

    public FGuid(uint v)
    {
        A = B = C = D = v;
    }

    public FGuid(uint a, uint b, uint c, uint d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }

    public FGuid(string hexString)
    {
        A = Convert.ToUInt32(hexString.Substring(0, 8), 16);
        B = Convert.ToUInt32(hexString.Substring(8, 8), 16);
        C = Convert.ToUInt32(hexString.Substring(16, 8), 16);
        D = Convert.ToUInt32(hexString.Substring(24, 8), 16);
    }

    public bool IsValid() => (A | B | C | D) != 0;

    public static bool operator ==(FGuid one, FGuid two) => one.A == two.A && one.B == two.B && one.C == two.C && one.D == two.D;
    public static bool operator !=(FGuid one, FGuid two) => one.A != two.A || one.B != two.B || one.C != two.C || one.D != two.D;

    public static implicit operator FGuid(Guid g) => new(g.ToString().Replace("-", ""));
}