using System.IO.Compression;
using System.Runtime.CompilerServices;
using K4os.Compression.LZ4;
using OodleDotNet;
using UAssetAPI.Pak.Readers;
using UAssetManager.Pak.Exceptions;
using ZlibngDotNet;
using ZstdSharp;

namespace UAssetAPI.Pak.Compression;
public static class Compression
{
    public const int LOADING_COMPRESSION_CHUNK_SIZE = 131072;

    public static byte[] Decompress(byte[] compressed, int uncompressedSize, CompressionMethod method, FArchive? reader = null) =>
        Decompress(compressed, 0, compressed.Length, uncompressedSize, method, reader);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decompress(byte[] compressed, int compressedOffset, int compressedCount, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
    {
        var uncompressed = new byte[uncompressedSize];
        Decompress(compressed, compressedOffset, compressedCount, uncompressed, 0, uncompressedSize, method);
        return uncompressed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Decompress(byte[] compressed, byte[] dst, CompressionMethod method, FArchive? reader = null) =>
        Decompress(compressed, 0, compressed.Length, dst, 0, dst.Length, method, reader);
    public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize, byte[] uncompressed, int uncompressedOffset, int uncompressedSize, CompressionMethod method, FArchive? reader = null)
    {
        var srcStream = new MemoryStream(compressed, compressedOffset, compressedSize, false);
        switch (method)
        {
            case CompressionMethod.None:
                Buffer.BlockCopy(compressed, compressedOffset, uncompressed, uncompressedOffset, compressedSize);
                return;
            case CompressionMethod.Zlib:
                ZlibHelper.Initialize();
                ZlibHelper.Decompress(compressed, compressedOffset, compressedSize, uncompressed, uncompressedOffset, uncompressedSize, reader);
                return;
            case CompressionMethod.Gzip:
                var gzip = new GZipStream(srcStream, CompressionMode.Decompress);
                gzip.Read(uncompressed, uncompressedOffset, uncompressedSize);
                gzip.Dispose();
                return;
            case CompressionMethod.Oodle:
                OodleHelper.Initialize();
                OodleHelper.Decompress(compressed, compressedOffset, compressedSize, uncompressed, uncompressedOffset, uncompressedSize, reader);
                return;
            case CompressionMethod.LZ4:
                var uncompressedBuffer = new byte[uncompressedSize + uncompressedSize / 255 + 16]; // LZ4_compressBound(uncompressedSize)
                int result;
#if USE_LZ4_NATIVE_LIB
                    unsafe
                    {
                        fixed (byte* compressedPtr = compressed, uncompressedBufferPtr = uncompressedBuffer)
                        {
                            result = LZ4.LZ4_decompress_safe(compressedPtr + compressedOffset, uncompressedBufferPtr, compressedSize, uncompressedBuffer.Length);
                        }
                    }
#else
                result = LZ4Codec.Decode(compressed, compressedOffset, compressedSize, uncompressedBuffer, 0, uncompressedBuffer.Length);
#endif
                Buffer.BlockCopy(uncompressedBuffer, 0, uncompressed, uncompressedOffset, uncompressedSize);
                if (result != uncompressedSize) throw new FileLoadException($"Failed to decompress LZ4 data (Expected: {uncompressedSize}, Result: {result})");
                return;
            case CompressionMethod.Zstd:
            {
                var compressionStream = new DecompressionStream(srcStream);
                compressionStream.Read(uncompressed, uncompressedOffset, uncompressedSize);
                compressionStream.Dispose();
                return;
            }
            default:
                if (reader != null) throw new UnknownCompressionMethodException(reader, $"Compression method \"{method}\" is unknown");
                else throw new UnknownCompressionMethodException($"Compression method \"{method}\" is unknown");
        }
    }

    public static byte[] Compress(byte[] uncompressed, int uncompressedSize, CompressionMethod method, int compressionLevel = 3)
    {
        switch (method)
        {
            case CompressionMethod.None:
                return uncompressed;

            case CompressionMethod.Zlib:
                //var srcStream = new MemoryStream();
                //var zlib = new ZlibStream(srcStream, CompressionMode.Compress, (CompressionLevel)compressionLevel, true);
                //zlib.Write(uncompressed, 0, uncompressedSize);
                //zlib.Flush();
                //zlib.Close();
                //return srcStream.ToArray();

                ZlibHelper.Initialize();
                return ZlibHelper.Compress(uncompressed, uncompressedSize, (ZlibngCompressionLevel)compressionLevel);

            case CompressionMethod.Gzip:
                var srcStream = new MemoryStream();
                var gzip = new GZipStream(srcStream, CompressionMode.Compress);
                gzip.Write(uncompressed, 0, uncompressedSize);
                gzip.Flush();
                gzip.Dispose();
                return srcStream.ToArray();

            case CompressionMethod.Oodle:
                OodleHelper.Initialize();
                return OodleHelper.Compress(uncompressed, uncompressedSize, OodleCompressor.Kraken, (OodleCompressionLevel)compressionLevel);

            case CompressionMethod.LZ4:
                var compressedBuffer = Array.Empty<byte>();
                LZ4Codec.Encode(uncompressed, compressedBuffer, (LZ4Level)compressionLevel);
                return compressedBuffer;

            default: throw new UnknownCompressionMethodException($"Compression method \"{method}\" is unknown");
        }
    }
}