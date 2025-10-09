using OffiUtils;
using System.Runtime.CompilerServices;
using UAssetManager.Pak.Exceptions;
using UAssetManager.Pak.Versions;

namespace UAssetManager.Pak.Readers;
public abstract class FArchive : RandomAccessStream, ICloneable
{
    public VersionContainer Versions;
    public EGame Game
    {
        get => Versions.Game;
        set => Versions.Game = value;
    }
    public FPackageFileVersion Ver
    {
        get => Versions.Ver;
        set => Versions.Ver = value;
    }
    public abstract string Name { get; }

    public override int ReadAt(long position, byte[] buffer, int offset, int count)
    {
        Position = position;
        CheckReadSize(count);

        return Read(buffer, offset, count);
    }

    public override Task<int> ReadAtAsync(long position, byte[] buffer, int offset, int count,
        CancellationToken cancellationToken = default)
    {
        Position = position;
        CheckReadSize(count);

        return ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAtAsync(long position, Memory<byte> memory, CancellationToken cancellationToken = default)
    {
        Position = position;
        CheckReadSize(memory.Length);

        return ReadAsync(memory, cancellationToken);
    }

    public virtual byte[] ReadBytes(int length)
    {
        CheckReadSize(length);

        var result = new byte[length];
        Read(result, 0, length);
        return result;
    }

    public virtual byte[] ReadBytesAt(long position, int length)
    {
        var result = new byte[length];
        ReadAt(position, result, 0, length);
        return result;
    }

    public virtual unsafe void Serialize(byte* ptr, int length)
    {
        var bytes = ReadBytes(length);
        Unsafe.CopyBlockUnaligned(ref ptr[0], ref bytes[0], (uint) length);
    }

    public virtual T Read<T>()
    {
        var size = Unsafe.SizeOf<T>();
        var buffer = ReadBytes(size);
        return Unsafe.ReadUnaligned<T>(ref buffer[0]);
    }

    public virtual T[] ReadArray<T>(int length)
    {
        var size = Unsafe.SizeOf<T>();
        var readLength = size * length;
        CheckReadSize(readLength);

        var buffer = ReadBytes(readLength);
        var result = new T[length];
        if (length > 0) Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref result[0]), ref buffer[0], (uint)readLength);
        return result;
    }

    public virtual void ReadArray<T>(T[] array)
    {
        if (array.Length == 0) return;
        var size = Unsafe.SizeOf<T>();
        var readLength = size * array.Length;
        CheckReadSize(readLength);

        var buffer = ReadBytes(readLength);
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<T, byte>(ref array[0]), ref buffer[0], (uint)readLength);
    }

    protected FArchive(VersionContainer? versions = null)
    {
        Versions = versions ?? new VersionContainer();
    }

    public override void Flush() { }
    public override bool CanRead { get; } = true;
    public override bool CanWrite { get; } = false;
    public override void SetLength(long value) { throw new InvalidOperationException(); }
    public override void Write(byte[] buffer, int offset, int count) { throw new InvalidOperationException(); }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadArray<T>(T[] array, Func<T> getter)
    {
        // array is a reference type
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = getter();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ReadArray<T>(int length, Func<T> getter)
    {
        if (length == 0) return [];

        var result = new T[length];
        ReadArray(result, getter);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual T[] ReadArray<T>(Func<T> getter)
    {
        var length = Read<int>();
        return ReadArray(length, getter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual T[] ReadArray<T>() where T : struct
    {
        var length = Read<int>();
        return length > 0 ? ReadArray<T>(length) : [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ReadBulkArray<T>(int elementSize, int elementCount, Func<T> getter)
    {
        var pos = Position;
        T[] array = ReadArray(elementCount, getter);
        return array;
    }

    public T[] ReadBulkArray<T>() where T : struct
    {
        var elementSize = Read<int>();
        var elementCount = Read<int>();
        if (elementCount == 0)
            return [];

        var pos = Position;
        T[] array = ReadArray<T>(elementCount);
        return array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ReadBulkArray<T>(Func<T> getter)
    {
        var elementSize = Read<int>();
        var elementCount = Read<int>();
        return ReadBulkArray(elementSize, elementCount, getter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipBulkArrayData()
    {
        var elementSize = Read<int>();
        var elementCount = Read<int>();
        Position += elementSize * elementCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipFixedArray(int size = -1)
    {
        var num = Read<int>();
        Position += num * size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(int length, Func<(TKey, TValue)> getter) where TKey : notnull
    {
        var res = new Dictionary<TKey, TValue>(length);
        for (var i = 0; i < length; i++)
        {
            var (key, value) = getter();
            res[key] = value;
        }

        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(int length, Func<TKey> keyGetter, Func<TValue> valueGetter) where TKey : notnull
    {
        var res = new Dictionary<TKey, TValue>(length);
        for (var i = 0; i < length; i++)
        {
            res[keyGetter()] = valueGetter();
        }

        return res;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(Func<TKey> keyGetter, Func<TValue> valueGetter) where TKey : notnull
    {
        var length = Read<int>();
        return ReadMap(length, keyGetter, valueGetter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<TKey, TValue> ReadMap<TKey, TValue>(Func<(TKey, TValue)> getter) where TKey : notnull
    {
        var length = Read<int>();
        return ReadMap(length, getter);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean()
    {
        var i = Read<int>();
        return i switch
        {
            0 => false,
            1 => true,
            _ => throw new ParserException(this, $"Invalid bool value ({i})")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadFlag()
    {
        var i = Read<byte>();
        return i switch
        {
            0 => false,
            1 => true,
            _ => throw new ParserException(this, $"Invalid bool value ({i})")
        };
    }

    public virtual uint ReadIntPacked()
    {
        uint value = 0;
        byte cnt = 0;
        bool more = true;
        while (more)
        {
            var nextByte = Read<byte>();               // Read next byte
            more = (nextByte & 1) != 0;                // Check 1 bit to see if there's more after this
            nextByte = (byte) (nextByte >> 1);         // Shift to get actual 7 bit value
            value += (uint) (nextByte << 7 * cnt++); // Add to total value
        }

        return value;
    }

    public virtual unsafe void SerializeBits(void* v, long lengthBits)
    {
        Serialize((byte*) v, (int) ((lengthBits + 7) / 8));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read7BitEncodedInt()
    {
        int count = 0, shift = 0;
        byte b;
        do
        {
            if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                throw new FormatException("Stream is corrupted");

            b = Read<byte>();
            count |= (b & 0x7F) << shift;
            shift += 7;
        } while ((b & 0x80) != 0);
        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
        var length = Read7BitEncodedInt();
        if (length <= 0)
            return string.Empty;

        unsafe
        {
            var ansiBytes = stackalloc byte[length];
            Serialize(ansiBytes, length);
            return new string((sbyte*) ansiBytes, 0, length);
        }
    }

    public virtual string ReadFString()
    {
        // > 0 for ANSICHAR, < 0 for UCS2CHAR serialization
        var length = Read<int>();

        if (length == int.MinValue)
            throw new ArgumentOutOfRangeException(nameof(length), "Archive is corrupted");

        if (Math.Abs(length) > Length - Position)
        {
            throw new ParserException($"Invalid FString length '{length}'");
        }

        if (length == 0)
        {
            return string.Empty;
        }

        // 1 byte/char is removed because of null terminator ('\0')
        if (length < 0) // LoadUCS2Char, Unicode, 16-bit, fixed-width
        {
            unsafe
            {
                length = -length;
                var ucs2Length = length * sizeof(ushort);
                var ucs2Bytes = ucs2Length <= 1024 ? stackalloc byte[ucs2Length] : new byte[ucs2Length];
                fixed (byte* ucs2BytesPtr = ucs2Bytes)
                {
                    Serialize(ucs2BytesPtr, ucs2Length);
                    return new string((char*) ucs2BytesPtr, 0 , length - 1);
                }
            }
        }

        unsafe
        {
            var ansiBytes = length <= 1024 ? stackalloc byte[length] : new byte[length];
            fixed (byte* ansiBytesPtr = ansiBytes)
            {
                Serialize(ansiBytesPtr, length);
                return new string((sbyte*) ansiBytesPtr, 0, length - 1);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong BYTESWAP_ORDER64(ulong value)
    {
        value = value << 8 & 0xFF00FF00FF00FF00UL | value >> 8 & 0x00FF00FF00FF00FFUL;
        value = value << 16 & 0xFFFF0000FFFF0000UL | value >> 16 & 0x0000FFFF0000FFFFUL;
        return value << 32 | value >> 32;
    }

    public void CheckReadSize(int length)
    {
        if (length < 0)
        {
            throw new ParserException(this, "Read size is smaller than zero.");
        }
        if (Position + length > Length)
        {
            throw new ParserException(this, "Read size is bigger than remaining archive length.");
        }
    }

    public abstract object Clone();

    private struct FCompressedChunkInfo
    {
        public long CompressedSize, UncompressedSize;
    }
}