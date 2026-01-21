using OodleDotNet;
using Serilog;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using UAssetAPI.Pak.Readers;
using UAssetManager.Pak.Exceptions;

namespace UAssetAPI.Pak.Compression;
public class OodleException : ParserException
{
    public OodleException(string? message = null, Exception? innerException = null) : base(message, innerException) { }
    public OodleException(FArchive reader, string? message = null, Exception? innerException = null) : base(reader, message, innerException) { }
}

public static class OodleHelper
{
    private const string WARFRAME_CONTENT_HOST = "https://content.warframe.com";
    private const string WARFRAME_ORIGIN_HOST = "https://origin.warframe.com";
    private const string WARFRAME_INDEX_PATH = "/origin/50F7040A/index.txt.lzma";
    private const string WARFRAME_INDEX_URL = WARFRAME_ORIGIN_HOST + WARFRAME_INDEX_PATH;
    public const string OODLE_DLL_NAME = "oo2core_9_win64.dll";

    public static Oodle? Instance { get; private set; }

    public static void Initialize()
    {
        if (Instance is not null) return;

        if (DownloadOodleDll())
        {
            Instance = new Oodle(OODLE_DLL_NAME);
        }
        else
        {
            Log.Warning("Oodle decompression failed: unable to download oodle dll");
        }
    }

    public static void Initialize(string path)
    {
        Instance?.Dispose();
        if (File.Exists(path))
            Instance = new Oodle(path);
    }

    public static void Initialize(Oodle instance)
    {
        Instance?.Dispose();
        Instance = instance;
    }

    public static bool DownloadOodleDll(string? path = null)
    {
        if (File.Exists(path ?? OODLE_DLL_NAME)) return true;
        return DownloadOodleDllAsync(path).GetAwaiter().GetResult();
    }

    public static void Decompress(byte[] compressed, int compressedOffset, int compressedSize,
        byte[] uncompressed, int uncompressedOffset, int uncompressedSize, FArchive? reader = null)
    {
        if (Instance is null)
        {
            const string message = "Oodle decompression failed: not initialized";
            if (reader is not null) throw new OodleException(reader, message);
            throw new OodleException(message);
        }

        var decodedSize = Instance.Decompress(compressed.AsSpan(compressedOffset, compressedSize),
            uncompressed.AsSpan(uncompressedOffset, uncompressedSize));

        if (decodedSize <= 0)
        {
            var message = $"Oodle decompression failed with result {decodedSize}";
            if (reader is not null) throw new OodleException(reader, message);
            throw new OodleException(message);
        }

        if (decodedSize < uncompressedSize)
        {
            // Not sure whether this should be an exception or not
            Log.Warning("Oodle decompression just decompressed {0} bytes of the expected {1} bytes", decodedSize, uncompressedSize);
        }
    }

    public static async Task<bool> DownloadOodleDllAsync(string? path)
    {
        path ??= OODLE_DLL_NAME;

        using var client = new HttpClient(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.All
        });
        client.Timeout = TimeSpan.FromSeconds(20);

        try
        {
            var ok = await DownloadOodleDllFromOodleUEAsync(client, path).ConfigureAwait(false);
            if (ok)
            {
                Log.Information($"Successfully downloaded oodle dll at \"{path}\"");
                return true;
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Uncaught exception while downloading oodle dll from alternative source");
        }

        return false;
    }

    public static async Task<bool> DownloadOodleDllFromOodleUEAsync(HttpClient client, string path)
    {
        const string url = "https://github.com/WorkingRobot/OodleUE/releases/download/2024-11-01-726/msvc.zip"; // 2.9.13
        const string entryName = "bin/Release/oodle-data-shared.dll";

        try
        {
            using var response = await client.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var zip = new ZipArchive(responseStream, ZipArchiveMode.Read);
            var entry = zip.GetEntry(entryName);
            ArgumentNullException.ThrowIfNull(entry, "oodle entry in zip not found");
            await using var entryStream = entry.Open();
            await using var fs = File.Create(path);
            await entryStream.CopyToAsync(fs).ConfigureAwait(false);
            return true;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Uncaught exception while downloading oodle dll from OodleUE");
        }
        return false;
    }

    public static byte[] Compress(byte[] uncompressed, int uncompressedSize, OodleCompressor format, OodleCompressionLevel level = OodleCompressionLevel.Fast)
    {
        if (Instance is null) throw new OodleException("Oodle compression failed: not initialized");

        int compressedSize = GetCompressionBound(uncompressedSize);
        byte[] compressed = new byte[compressedSize];

        var encodedSize = Instance.Compress(format, level, uncompressed.AsSpan(0, uncompressedSize),
            compressed.AsSpan(0, compressedSize));

        byte[] outputBuffer = new byte[encodedSize];
        Buffer.BlockCopy(compressed, 0, outputBuffer, 0, (int)encodedSize);

        return outputBuffer;
    }

    private static int GetCompressionBound(int bufferSize)
    {
        return bufferSize + 274 * ((bufferSize + 0x3FFFF) / 0x40000);
    }
}