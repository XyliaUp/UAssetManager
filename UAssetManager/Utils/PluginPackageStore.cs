using Serilog;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UAssetManager.Utils;

internal sealed record PluginPackageManifest(int Schema, string Id, string Name, string Version, string EntryAssembly, string MinHostVersion);
internal sealed record PluginPackageState(string Repository, string Current, string? Previous);
internal sealed record InstalledPluginPackage(string EntryPath, string AssemblyName);

internal static class PluginPackageStore
{
    private const string StateFileName = "installed.json";
    private const string ManifestFileName = "plugin.json";
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private static readonly Regex _packageId = new(@"\A[a-zA-Z0-9][a-zA-Z0-9.-]{0,79}\z");
    private static readonly Regex _revision = new(@"\A[a-f0-9]{64}(?:-[a-f0-9]{32})?\z");

    public static string DefaultDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UAssetManager", "Plugins");

    public static PluginPackageManifest ReadManifest(string directory, Version hostVersion)
    {
        var file = new FileInfo(Path.Combine(directory, ManifestFileName));
        if (!file.Exists || file.Length > 64 * 1024)
            throw new PluginPackageException("PluginPackage_InvalidBundle");

        try
        {
            var manifest = JsonSerializer.Deserialize<PluginPackageManifest>(File.ReadAllText(file.FullName), _json);
            if (manifest is null || manifest.Schema != 1 || !IsValidId(manifest.Id) || string.IsNullOrWhiteSpace(manifest.Name)
                || !Version.TryParse(manifest.Version, out _) || !Version.TryParse(manifest.MinHostVersion, out var minimumHost)
                || !IsSafeFileName(manifest.EntryAssembly) || !manifest.EntryAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || !File.Exists(Path.Combine(directory, manifest.EntryAssembly)))
            {
                throw new PluginPackageException("PluginPackage_InvalidBundle");
            }

            if (hostVersion < minimumHost)
                throw new PluginPackageException("PluginPackage_HostTooOld", minimumHost, hostVersion);

            return manifest with { Id = manifest.Id.ToLowerInvariant() };
        }
        catch (JsonException)
        {
            throw new PluginPackageException("PluginPackage_InvalidBundle");
        }
    }

    public static InstalledPluginPackage DescribeEntry(string directory, PluginPackageManifest manifest)
    {
        var path = Path.Combine(directory, manifest.EntryAssembly);
        try
        {
            var name = AssemblyName.GetAssemblyName(path).Name;
            if (string.IsNullOrWhiteSpace(name))
                throw new PluginPackageException("PluginPackage_InvalidBundle");

            return new InstalledPluginPackage(path, name);
        }
        catch (BadImageFormatException)
        {
            throw new PluginPackageException("PluginPackage_InvalidBundle");
        }
    }

    public static bool IsSafeFileName(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith('.') || name.EndsWith(' '))
            return false;

        var stem = name.Split('.', 2)[0].TrimEnd().ToUpperInvariant();
        return stem is not ("CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$") &&
            !(stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                && "0123456789¹²³".Contains(stem[3]));
    }

    private static bool IsValidId(string? id)
    {
        return id is not null && _packageId.IsMatch(id) && IsSafeFileName(id);
    }

    public static PluginPackageState? ReadState(string packageDirectory)
    {
        var path = Path.Combine(packageDirectory, StateFileName);
        if (!File.Exists(path))
            return null;

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        try
        {
            var state = JsonSerializer.Deserialize<PluginPackageState>(stream, _json);
            if (state is null || string.IsNullOrWhiteSpace(state.Repository) || state.Current is null || !_revision.IsMatch(state.Current)
                || (state.Previous is not null && !_revision.IsMatch(state.Previous)))
            {
                throw new PluginPackageException("PluginPackage_InvalidBundle");
            }

            return state;
        }
        catch (JsonException)
        {
            throw new PluginPackageException("PluginPackage_InvalidBundle");
        }
    }

    public static void WriteState(string packageDirectory, PluginPackageState state)
    {
        var temporary = Path.Combine(packageDirectory, "installed.tmp");
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, _json));
        File.Move(temporary, Path.Combine(packageDirectory, StateFileName), overwrite: true);
    }

    public static IReadOnlyList<InstalledPluginPackage> ReadInstalled(string directory, Version hostVersion)
    {
        var packages = new List<InstalledPluginPackage>();
        if (!Directory.Exists(directory))
            return packages;

        string[] directories;
        try
        {
            directories = Directory.GetDirectories(directory);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Log.Warning(error, "Could not read managed plugins directory {Directory}.", directory);
            return packages;
        }

        foreach (var packageDirectory in directories)
        {
            var id = Path.GetFileName(packageDirectory);
            if (!IsValidId(id))
                continue;

            try
            {
                var state = ReadState(packageDirectory);
                if (state is not null)
                {
                    var package = TryReadRevision(packageDirectory, state.Current, hostVersion)
                        ?? TryReadRevision(packageDirectory, state.Previous, hostVersion);
                    if (package is { } valid)
                        packages.Add(valid.Entry);
                }
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Log.Warning(error, "Could not read plugin package {PluginId}.", id);
            }
        }

        return packages;
    }

    public static (PluginPackageManifest Manifest, InstalledPluginPackage Entry)? TryReadRevision(string packageDirectory, string? revision, Version hostVersion)
    {
        if (revision is null)
            return null;

        try
        {
            var root = Path.Combine(packageDirectory, revision);
            var manifest = ReadManifest(root, hostVersion);
            if (!string.Equals(manifest.Id, Path.GetFileName(packageDirectory), StringComparison.OrdinalIgnoreCase))
                throw new PluginPackageException("PluginPackage_InvalidBundle");

            return (manifest, DescribeEntry(root, manifest));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Log.Warning(error, "Could not load plugin package files from {Directory}/{Revision}.", packageDirectory, revision);
            return null;
        }
    }
}
