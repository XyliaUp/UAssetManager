using System.IO;
using System.Runtime.CompilerServices;

namespace UAssetManager.Utils;
public static class BinaryExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Read<T>(this BinaryReader reader)
    {
        var size = Unsafe.SizeOf<T>();
        var buffer = reader.ReadBytes(size);
        return Unsafe.ReadUnaligned<T>(ref buffer[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadArray<T>(this BinaryReader reader) => ReadArray(reader, () => Read<T>(reader));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] ReadArray<T>(this BinaryReader reader, Func<T> getter)
    {
        var length = reader.Read<int>();
        var result = new T[length];

        // array is a reference type
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = getter();
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this BinaryWriter writer, T value) where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        var data = new byte[size];

        unsafe
        {
            fixed (byte* p = &Unsafe.As<T, byte>(ref value))
            {
                using var ms = new UnmanagedMemoryStream(p, size);
                ms.Read(data, 0, data.Length);
            }
        }

        writer.Write(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray<T>(this BinaryWriter writer, T[] value) where T : struct
    {
        WriteArray(writer, value, (o) => Write(writer, o));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteArray<T>(this BinaryWriter writer, T[] value, Action<T> writter)
    {
        writer.Write(value.Length);

        for (int i = 0; i < value.Length; i++)
        {
            writter(value[i]);
        }
    }
}