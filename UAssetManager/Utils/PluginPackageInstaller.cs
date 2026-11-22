using Serilog;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UAssetManager.Utils;

internal sealed record PluginPackageRelease(string Repository, string Version, string ArchiveName, long SizeBytes, Uri DownloadUri, string? Sha256);
internal sealed record PluginPackageProgress(long BytesDownloaded, long TotalBytes);

internal sealed record PluginPackageInstallResult(string Id, string Name, string Version)
{
    public bool RequiresRestart => true;
}

internal sealed class PluginPackageException(string resourceKey, params object?[] resourceArguments) : IOException(resourceKey)
{
    public string ResourceKey { get; } = resourceKey;
    public object?[] ResourceArguments { get; } = resourceArguments;
}

internal sealed class PluginPackageInstaller
{
    private const long MaxDownloadBytes = 512L * 1024 * 1024;
    private const long MaxExtractedBytes = 2L * 1024 * 1024 * 1024;
    private static readonly Regex _repositoryPath = new(@"\A/(?<owner>[a-zA-Z0-9][a-zA-Z0-9-]{0,38})/(?<repo>[a-zA-Z0-9_.-]{1,100})/?\z");
    private static readonly Regex _sha256 = new(@"\Asha256:[a-fA-F0-9]{64}\z");
    private static readonly HttpClient _githubClient = new() { MaxResponseContentBufferSize = 2 * 1024 * 1024 };

    private readonly HttpClient _client;
    private readonly string _directory;
    private readonly Version _hostVersion;

    public PluginPackageInstaller() : this(_githubClient, PluginPackageStore.DefaultDirectory, typeof(PluginPackageInstaller).Assembly.GetName().Version!)
    {
    }

    internal PluginPackageInstaller(HttpClient client, string directory, Version hostVersion)
    {
        _client = client;
        _directory = directory;
        _hostVersion = hostVersion;
    }

    public async Task<PluginPackageRelease> FindReleaseAsync(string repositoryUrl, CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PluginPackageException("PluginPackage_UnsupportedPlatform");

        var repository = GetRepository(repositoryUrl);
        using var request = CreateRequest(new Uri($"https://api.github.com/repos/{repository}/releases/latest"));
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new PluginPackageException("PluginPackage_ReleaseNotFound");

        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ReadRelease(repository, document.RootElement);
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new PluginPackageException("PluginPackage_InvalidRelease");
        }
    }

    public async Task<PluginPackageInstallResult> InstallAsync(PluginPackageRelease release, IProgress<PluginPackageProgress>? progress, CancellationToken cancellationToken)
    {
        var stagingRoot = Path.Combine(_directory, ".staging");
        Directory.CreateDirectory(stagingRoot);
        var staging = Path.Combine(stagingRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var zip = Path.Combine(staging, "download.zip");
            var revision = await DownloadAsync(release, zip, progress, cancellationToken).ConfigureAwait(false);
            var payload = Path.Combine(staging, "payload");
            Directory.CreateDirectory(payload);
            await ExtractAsync(zip, payload, cancellationToken).ConfigureAwait(false);
            var manifest = PluginPackageStore.ReadManifest(payload, _hostVersion);
            PluginPackageStore.DescribeEntry(payload, manifest);
            cancellationToken.ThrowIfCancellationRequested();
            Install(release, payload, revision, manifest, cancellationToken);
            return new PluginPackageInstallResult(manifest.Id, manifest.Name, manifest.Version);
        }
        finally
        {
            try
            {
                Directory.Delete(staging, recursive: true);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Log.Warning(error, "Could not remove plugin package staging directory {Directory}.", staging);
            }
        }
    }

    private static string GetRepository(string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) || !uri.IsDefaultPort || uri.UserInfo.Length != 0
            || uri.Query.Length != 0 || uri.Fragment.Length != 0)
        {
            throw new PluginPackageException("PluginPackage_InvalidRepository");
        }

        var match = _repositoryPath.Match(uri.AbsolutePath);
        var repo = match.Groups["repo"].Value;
        if (!match.Success || repo is "." or "..")
            throw new PluginPackageException("PluginPackage_InvalidRepository");

        return match.Groups["owner"].Value + "/" + repo;
    }

    private static PluginPackageRelease ReadRelease(string repository, JsonElement release)
    {
        var tag = release.GetProperty("tag_name").GetString();
        if (string.IsNullOrWhiteSpace(tag))
            throw new PluginPackageException("PluginPackage_InvalidRelease");

        var archiveName = repository[(repository.IndexOf('/') + 1)..] + "-win-x64.zip";
        foreach (var asset in release.GetProperty("assets").EnumerateArray())
        {
            if (!string.Equals(asset.GetProperty("name").GetString(), archiveName, StringComparison.OrdinalIgnoreCase))
                continue;

            var size = asset.GetProperty("size").GetInt64();
            if (size <= 0 || size > MaxDownloadBytes || !Uri.TryCreate(asset.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var downloadUri)
                || downloadUri.Scheme != Uri.UriSchemeHttps || !string.Equals(downloadUri.Host, "github.com", StringComparison.OrdinalIgnoreCase)
                || !downloadUri.IsDefaultPort || downloadUri.UserInfo.Length != 0 || downloadUri.Query.Length != 0 || downloadUri.Fragment.Length != 0
                || !downloadUri.AbsolutePath.StartsWith($"/{repository}/releases/download/", StringComparison.OrdinalIgnoreCase)
                || !downloadUri.AbsolutePath.EndsWith("/" + archiveName, StringComparison.OrdinalIgnoreCase))
            {
                throw new PluginPackageException("PluginPackage_InvalidRelease");
            }

            var reportedDigest = asset.TryGetProperty("digest", out var digest) ? digest.GetString() : null;
            if (reportedDigest is not null && !_sha256.IsMatch(reportedDigest))
                throw new PluginPackageException("PluginPackage_InvalidRelease");

            return new PluginPackageRelease(repository, tag, archiveName, size, downloadUri, reportedDigest?[7..]);
        }

        throw new PluginPackageException("PluginPackage_AssetNotFound", archiveName);
    }

    private static HttpRequestMessage CreateRequest(Uri uri)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.UserAgent.ParseAdd("UAssetManager-PluginInstaller");
        return request;
    }

    private async Task<string> DownloadAsync(PluginPackageRelease release, string path, IProgress<PluginPackageProgress>? progress, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(release.DownloadUri);
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var output = File.Create(path);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long total = 0;
        long lastReport = 0;
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            total += count;
            if (total > release.SizeBytes)
                throw new PluginPackageException("PluginPackage_InvalidBundle");

            hash.AppendData(buffer.AsSpan(0, count));
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
            if (total - lastReport >= 512 * 1024)
            {
                progress?.Report(new PluginPackageProgress(total, release.SizeBytes));
                lastReport = total;
            }
        }

        var revision = Convert.ToHexStringLower(hash.GetHashAndReset());
        if (total != release.SizeBytes || (release.Sha256 is not null && !revision.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase)))
            throw new PluginPackageException("PluginPackage_InvalidBundle");

        progress?.Report(new PluginPackageProgress(total, release.SizeBytes));
        return revision;
    }

    private static async Task ExtractAsync(string zip, string payload, CancellationToken cancellationToken)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zip);
            if (archive.Entries.Count > 8192)
                throw new PluginPackageException("PluginPackage_InvalidBundle");

            long total = 0;
            var buffer = new byte[81920];
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = entry.FullName.Replace('\\', '/');
                bool directory = name.EndsWith('/');
                var components = (directory ? name[..^1] : name).Split('/');
                var fileType = (entry.ExternalAttributes >> 16) & 0xF000;
                if (components.Any(component => !PluginPackageStore.IsSafeFileName(component))
                    || fileType == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                {
                    throw new PluginPackageException("PluginPackage_InvalidBundle");
                }

                var path = Path.Combine(payload, Path.Combine(components));
                if (directory)
                {
                    Directory.CreateDirectory(path);
                    continue;
                }

                if (entry.Length > MaxExtractedBytes - total || File.Exists(path) || Directory.Exists(path))
                    throw new PluginPackageException("PluginPackage_InvalidBundle");

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await using var input = entry.Open();
                await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, buffer.Length, useAsync: true);
                long remaining = entry.Length;
                int count;
                while ((count = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    remaining -= count;
                    if (remaining < 0)
                        throw new PluginPackageException("PluginPackage_InvalidBundle");

                    await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                }

                if (remaining != 0)
                    throw new PluginPackageException("PluginPackage_InvalidBundle");

                total += entry.Length;
            }
        }
        catch (InvalidDataException)
        {
            throw new PluginPackageException("PluginPackage_InvalidBundle");
        }
    }

    private void Install(PluginPackageRelease release, string payload, string revision, PluginPackageManifest manifest, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_directory, manifest.Id);
        Directory.CreateDirectory(directory);
        using var installLock = new FileStream(Path.Combine(directory, "install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var previous = PluginPackageStore.ReadState(directory);
        if (previous is not null && !previous.Repository.Equals(release.Repository, StringComparison.OrdinalIgnoreCase))
            throw new PluginPackageException("PluginPackage_IdConflict", manifest.Id);

        var current = PluginPackageStore.TryReadRevision(directory, previous?.Current, _hostVersion);
        var fallback = current is null ? PluginPackageStore.TryReadRevision(directory, previous?.Previous, _hostVersion) : null;
        var installedVersion = (current ?? fallback)?.Manifest.Version;
        if (installedVersion is not null && Version.Parse(manifest.Version) < Version.Parse(installedVersion))
            throw new PluginPackageException("PluginPackage_VersionTooOld", manifest.Version, installedVersion);

        if (current is not null && previous?.Current == revision)
            return;

        var destination = Path.Combine(directory, revision);
        if (Directory.Exists(destination))
        {
            revision += "-" + Guid.NewGuid().ToString("N");
            destination = Path.Combine(directory, revision);
        }
        Directory.Move(payload, destination);

        cancellationToken.ThrowIfCancellationRequested();
        var usablePrevious = current is not null ? previous?.Current : fallback is not null ? previous?.Previous : null;
        PluginPackageStore.WriteState(directory, new PluginPackageState(release.Repository, revision, usablePrevious));
    }
}
