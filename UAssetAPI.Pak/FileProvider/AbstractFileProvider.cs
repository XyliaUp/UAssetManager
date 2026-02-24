using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Serilog;
using UAssetAPI.Pak.FileProvider.Objects;
using UAssetAPI.Pak.FileProvider.Vfs;
using UAssetAPI.Pak.Objects;
using UAssetAPI.Pak.Objects.Core.Misc;
using UAssetAPI.Pak.Pak.Utils;
using UAssetAPI.Pak.Readers;
using UAssetAPI.Pak.Versions;
using UAssetAPI.UnrealTypes;
using UAssetAPI.UnrealTypes.EngineEnums;
using UAssetAPI.Unversioned;

namespace UAssetAPI.Pak.FileProvider
{
	public class CustomConfigIni
	{
		/// <summary>
		/// guid of the archive that owns this config
		/// </summary>
		public FGuid? EncryptionKeyGuid { get; set; }

		public CustomConfigIni(string name) { }
	}

	public abstract class AbstractFileProvider : IFileProvider
	{
		protected static readonly ILogger Log = Serilog.Log.ForContext<IFileProvider>();

		public VersionContainer Versions { get; }
		public StringComparer PathComparer { get; }

		public FileProviderDictionary Files { get; }
		public InternationalizationDictionary Internationalization { get; }
		public IDictionary<string, string> VirtualPaths { get; }
		public CustomConfigIni DefaultGame { get; }
		public CustomConfigIni DefaultEngine { get; }

		public ELightUnits DefaultLightUnit { get; set; } = ELightUnits.Unitless;

		public Usmap? MappingsContainer { get; set; }
		public bool ReadScriptData { get; set; }
		public bool ReadShaderMaps { get; set; }
		public bool SkipReferencedTextures { get; set; }
		public bool UseLazyPackageSerialization { get; set; } = true;


		protected AbstractFileProvider(VersionContainer? versions = null, StringComparer? pathComparer = null)
		{
			Versions = versions ?? VersionContainer.DEFAULT_VERSION_CONTAINER;
			PathComparer = pathComparer ?? StringComparer.Ordinal;

			Files = new FileProviderDictionary();
			Internationalization = new InternationalizationDictionary(PathComparer);
			VirtualPaths = new Dictionary<string, string>(PathComparer);
			DefaultGame = new CustomConfigIni(nameof(DefaultGame));
			DefaultEngine = new CustomConfigIni(nameof(DefaultEngine));
		}

		private string? _gameDisplayName;
		public string? GameDisplayName => _gameDisplayName;


		private string? _projectName;
		public string ProjectName
		{
			get
			{
				if (string.IsNullOrEmpty(_projectName))
				{
					if (Files.Keys.FirstOrDefault(it => it.EndsWith(".uproject", StringComparison.OrdinalIgnoreCase)) is not { } t)
					{
						t = Files.Keys.FirstOrDefault(
							it => !it.StartsWith('/') && it.Contains('/') &&
								  !it.SubstringBefore('/').EndsWith("Engine", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
					}

					_projectName = t.SubstringBefore('/');
					if (PathComparer.Equals(_projectName, "MidnightSuns"))
						_projectName = "CodaGame";
				}
				return _projectName;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected bool TryGetGameFile(string path, IReadOnlyDictionary<string, GameFile> collection, [MaybeNullWhen(false)] out GameFile file)
		{
			var fixedPath = FixPath(path);
			if (!collection.TryGetValue(fixedPath, out file) && // any extension
				!collection.TryGetValue(fixedPath.SubstringBeforeWithLast('.') + GameFile.UePackageExtensions[1], out file) && // umap
				!collection.TryGetValue(path, out file)) // in case FixPath broke something
			{
				file = null;
			}

			return file != null;
		}

		public GameFile this[string path]
			=> TryGetGameFile(path, Files, out var file)
				? file
				: throw new KeyNotFoundException($"There is no game file with the path \"{path}\"");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual bool TryGetGameFile(string path, [MaybeNullWhen(false)] out GameFile file)
		{
			try
			{
				file = this[path];
			}
			catch
			{
				file = null;
			}
			return file != null;
		}

		public void ChangeCulture(string culture) => Internationalization.ChangeCulture(culture, Files);

		public bool TryChangeCulture(string culture)
		{
			try
			{
				ChangeCulture(culture);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public int LoadVirtualPaths() { return LoadVirtualPaths(Versions.Ver); }
		public int LoadVirtualPaths(FPackageFileVersion version, CancellationToken cancellationToken = default)
		{
			var regex = new Regex($"^{ProjectName}/Plugins/.+.upluginmanifest$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			VirtualPaths.Clear();

			var i = 0;
			var useIndividualPlugin = version < EUnrealEngineObjectUE4Version.ADDED_SOFT_OBJECT_PATH || !Files.Any(file => file.Key.EndsWith(".upluginmanifest"));
			foreach ((string filePath, GameFile gameFile) in Files)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}

			return i;
		}

		public virtual string FixPath(string path)
		{
			path = path.Replace('\\', '/');
			if (path[0] == '/')
				path = path[1..]; // remove leading slash

			var lastPart = path.SubstringAfterLast('/');
			// This part is only for FSoftObjectPaths and not really needed anymore internally, but it's still in here for user input
			if (lastPart.Contains('.') && lastPart.SubstringBefore('.') == lastPart.SubstringAfter('.'))
				path = string.Concat(path.SubstringBeforeWithLast('/'), lastPart.SubstringBefore('.'));
			if (path[^1] != '/' && !lastPart.Contains('.'))
				path += "." + GameFile.UePackageExtensions[0]; // uasset

			var ret = path;
			var root = path.SubstringBefore('/');
			var tree = path.SubstringAfter('/');
			if (PathComparer.Equals(root, "Game") || PathComparer.Equals(root, "Engine"))
			{
				var projectName = PathComparer.Equals(root, "Engine") ? "Engine" : ProjectName;
				var root2 = tree.SubstringBefore('/');
				if (PathComparer.Equals(root2, "Config") ||
					PathComparer.Equals(root2, "Content") ||
					PathComparer.Equals(root2, "Plugins"))
				{
					ret = string.Concat(projectName, '/', tree);
				}
				else
				{
					ret = string.Concat(projectName, "/Content/", tree);
				}
			}
			else if (PathComparer.Equals(root, ProjectName))
			{
				// everything should be good
			}
			else if (VirtualPaths.TryGetValue(root, out var use))
			{
				ret = string.Concat(use, "/Content/", tree);
			}
			else if (PathComparer.Equals(ProjectName, "FortniteGame"))
			{
				ret = string.Concat(ProjectName, $"/Plugins/GameFeatures/{root}/Content/", tree);
			}

			return ret;
		}

		#region SaveAsset Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] SaveAsset(string path) => SaveAsset(this[path]);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte[] SaveAsset(GameFile file) => file.Read();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Task<byte[]> SaveAssetAsync(string path) => SaveAssetAsync(this[path]);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<byte[]> SaveAssetAsync(GameFile file) => await file.ReadAsync().ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TrySaveAsset(string path, [MaybeNullWhen(false)] out byte[] data)
		{
			if (TryGetGameFile(path, out var file))
			{
				return TrySaveAsset(file, out data);
			}

			data = null;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TrySaveAsset(GameFile file, [MaybeNullWhen(false)] out byte[] data)
		{
			data = file.SafeRead();
			return data != null;
		}
		#endregion

		#region CreateReader Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FArchive CreateReader(string path) => this[path].CreateReader();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Task<FArchive> CreateReaderAsync(string path) => this[path].CreateReaderAsync();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryCreateReader(string path, [MaybeNullWhen(false)] out FArchive reader)
		{
			reader = null;
			if (TryGetGameFile(path, out var file))
			{
				reader = file.SafeCreateReader();
			}

			return reader != null;
		}
		#endregion

		#region LoadPackage Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UAsset LoadPackage(string path) => LoadPackage(this[path]);
		public UAsset LoadPackage(GameFile file)
		{
			if (!file.IsUePackage)
				throw new ArgumentException("cannot load non-UE package", nameof(file));
			Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls);

			var uasset = file.CreateReader();
			var lazyUbulk = ubulks.Count > 0 ? new Lazy<FArchive?>(() => ubulks[0].SafeCreateReader()) : null;
			var lazyUptnl = uptnls.Count > 0 ? new Lazy<FArchive?>(() => uptnls[0].SafeCreateReader()) : null;

			switch (file)
			{
				case FPakEntry or OsGameFile:
				{
					using var memory = new MemoryStream(file.Read());
					using var reader = new AssetBinaryReader(memory);
					var useSeparateBulkDataFiles = uexp != null || ubulks.Count > 0 || uptnls.Count > 0;
					return new UAsset(reader, EngineVersion.VER_UE4_24, MappingsContainer, useSeparateBulkDataFiles);
				}
				//case FIoStoreEntry ioStoreEntry when this is IVfsFileProvider vfsFileProvider:
				//	return new IoPackage(uasset, ioStoreEntry.IoStoreReader.ContainerHeader, lazyUbulk, lazyUptnl, vfsFileProvider);
				default:
					throw new NotImplementedException($"type {file.GetType()} is not supported");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Task<UAsset> LoadPackageAsync(string path) => LoadPackageAsync(this[path]);
		public async Task<UAsset> LoadPackageAsync(GameFile file)
		{
			if (!file.IsUePackage)
				throw new ArgumentException("cannot load non-UE package", nameof(file));
			Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls);

			var uasset = await file.CreateReaderAsync().ConfigureAwait(false);
			var lazyUbulk = ubulks.Count > 0 ? new Lazy<FArchive?>(() => ubulks[0].SafeCreateReader()) : null;
			var lazyUptnl = uptnls.Count > 0 ? new Lazy<FArchive?>(() => uptnls[0].SafeCreateReader()) : null;

			switch (file)
			{
				case FPakEntry or OsGameFile:
				{
					using var memory = new MemoryStream(file.Read());
					using var reader = new AssetBinaryReader(memory);
					var useSeparateBulkDataFiles = uexp != null || ubulks.Count > 0 || uptnls.Count > 0;
					return new UAsset(reader, EngineVersion.VER_UE4_24, MappingsContainer, useSeparateBulkDataFiles);
				}
				//case FIoStoreEntry ioStoreEntry when this is IVfsFileProvider vfsFileProvider:
				//	return new IoPackage(uasset, ioStoreEntry.IoStoreReader.ContainerHeader, lazyUbulk, lazyUptnl, vfsFileProvider);
				default:
					throw new NotImplementedException($"type {file.GetType()} is not supported");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLoadPackage(string path, [MaybeNullWhen(false)] out UAsset package)
		{
			if (TryGetGameFile(path, out var file))
			{
				return TryLoadPackage(file, out package);
			}

			package = null;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLoadPackage(GameFile file, [MaybeNullWhen(false)] out UAsset package)
		{
			try
			{
				package = LoadPackage(file);
			}
			catch
			{
				package = null;
			}
			return package != null;
		}
		#endregion

		#region SavePackage Methods
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IReadOnlyDictionary<string, byte[]> SavePackage(string path) => SavePackage(this[path]);
		public IReadOnlyDictionary<string, byte[]> SavePackage(GameFile file)
		{
			Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls, true);

			var dict = new Dictionary<string, byte[]> { { file.Path, file.Read() } };
			if (uexp != null)
				dict[uexp.Path] = uexp.Read();
			foreach (var ubulk in ubulks)
				dict[ubulk.Path] = ubulk.Read();
			foreach (var uptnl in uptnls)
				dict[uptnl.Path] = uptnl.Read();

			return dict;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(string path) => await SavePackageAsync(this[path]).ConfigureAwait(false);
		public async Task<IReadOnlyDictionary<string, byte[]>> SavePackageAsync(GameFile file)
		{
			Files.FindPayloads(file, out var uexp, out var ubulks, out var uptnls, true);

			var dict = new Dictionary<string, byte[]> { { file.Path, await file.ReadAsync().ConfigureAwait(false) } };
			if (uexp != null)
				dict[uexp.Path] = await uexp.ReadAsync().ConfigureAwait(false);
			foreach (var ubulk in ubulks)
				dict[ubulk.Path] = await ubulk.ReadAsync().ConfigureAwait(false);
			foreach (var uptnl in uptnls)
				dict[uptnl.Path] = await uptnl.ReadAsync().ConfigureAwait(false);

			return dict;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TrySavePackage(string path, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data)
		{
			if (TryGetGameFile(path, out var file))
			{
				return TrySavePackage(file, out data);
			}

			data = null;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TrySavePackage(GameFile file, [MaybeNullWhen(false)] out IReadOnlyDictionary<string, byte[]> data)
		{
			try
			{
				data = SavePackage(file);
			}
			catch
			{
				data = null;
			}
			return data != null;
		}
		#endregion

		#region LoadObject Methods
		protected virtual ValueTuple<string, string> GetPathName(string path)
		{
			var index = path.LastIndexOf('.');
			string objectName;
			if (index == -1)
			{
				objectName = path.SubstringAfterLast('/');
			}
			else
			{
				objectName = path[(index + 1)..];
				path = path[..index];
			}
			return (path, objectName);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UObject LoadPackageObject(string path) => LoadPackageObject<UObject>(path);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T LoadPackageObject<T>(string path) where T : UObject => LoadPackageObject<T>(GetPathName(path));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UObject LoadPackageObject(string path, string objectName) => LoadPackageObject<UObject>(path, objectName);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T LoadPackageObject<T>(string path, string objectName) where T : UObject => LoadPackageObject<T>((path, objectName));

		private T? LoadPackageObject<T>(ValueTuple<string, string> pathName) where T : UObject
		{
			ArgumentException.ThrowIfNullOrEmpty("path", pathName.Item1);
			ArgumentException.ThrowIfNullOrEmpty("objectName", pathName.Item2);

			var package = LoadPackage(pathName.Item1);
			return (T?)package.Exports.FirstOrDefault(x => x.ObjectName.ToString() == pathName.Item2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<UObject> LoadPackageObjectAsync(string path) => await LoadPackageObjectAsync<UObject>(path).ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<T> LoadPackageObjectAsync<T>(string path) where T : UObject => await LoadPackageObjectAsync<T>(GetPathName(path)).ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<UObject> LoadPackageObjectAsync(string path, string objectName) => await LoadPackageObjectAsync<UObject>(path, objectName).ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<T> LoadPackageObjectAsync<T>(string path, string objectName) where T : UObject => await LoadPackageObjectAsync<T>((path, objectName)).ConfigureAwait(false);

		private async Task<T> LoadPackageObjectAsync<T>(ValueTuple<string, string> pathName) where T : UObject
		{
			ArgumentException.ThrowIfNullOrEmpty("path", pathName.Item1);
			ArgumentException.ThrowIfNullOrEmpty("objectName", pathName.Item2);

			var package = await LoadPackageAsync(pathName.Item1).ConfigureAwait(false);
			return (T?)package.Exports.FirstOrDefault(x => x.ObjectName.ToString() == pathName.Item2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UObject? SafeLoadPackageObject(string path) => SafeLoadPackageObject<UObject>(path);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T? SafeLoadPackageObject<T>(string path) where T : UObject => SafeLoadPackageObject<T>(GetPathName(path));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UObject? SafeLoadPackageObject(string path, string objectName) => SafeLoadPackageObject<UObject>(path, objectName);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T? SafeLoadPackageObject<T>(string path, string objectName) where T : UObject => SafeLoadPackageObject<T>((path, objectName));

		private T? SafeLoadPackageObject<T>(ValueTuple<string, string> pathName) where T : UObject
		{
			try
			{
				return LoadPackageObject<T>(pathName);
			}
			catch
			{
				return null;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<UObject?> SafeLoadPackageObjectAsync(string path) => await SafeLoadPackageObjectAsync<UObject>(path).ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<T?> SafeLoadPackageObjectAsync<T>(string path) where T : UObject => await SafeLoadPackageObjectAsync<T>(GetPathName(path)).ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<UObject?> SafeLoadPackageObjectAsync(string path, string objectName) => await SafeLoadPackageObjectAsync<UObject>(path, objectName).ConfigureAwait(false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public async Task<T?> SafeLoadPackageObjectAsync<T>(string path, string objectName) where T : UObject => await SafeLoadPackageObjectAsync<T>((path, objectName)).ConfigureAwait(false);

		private async Task<T?> SafeLoadPackageObjectAsync<T>(ValueTuple<string, string> pathName) where T : UObject
		{
			try
			{
				return await LoadPackageObjectAsync<T>(pathName).ConfigureAwait(false);
			}
			catch
			{
				return null;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLoadPackageObject(string path, [MaybeNullWhen(false)] out UObject export) => TryLoadPackageObject<UObject>(path, out export);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryLoadPackageObject<T>(string path, [MaybeNullWhen(false)] out T export) where T : UObject
		{
			export = SafeLoadPackageObject<T>(path);
			return export != null;
		}
		#endregion

		public virtual void Dispose()
		{
			Files.Clear();
			VirtualPaths.Clear();
			Internationalization.Clear();
		}
	}
}
