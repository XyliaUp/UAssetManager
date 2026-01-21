using CUE4Parse.FileProvider.Vfs;
using UAssetAPI.Pak.Versions;

namespace UAssetAPI.Pak.FileProvider;
public class StreamedFileProvider : AbstractVfsFileProvider
{
	public string LiveGame { get; }

	public StreamedFileProvider(string liveGame, VersionContainer? versions = null, StringComparer? pathComparer = null) : base(versions, pathComparer)
	{
		LiveGame = liveGame;
	}

	public override void Initialize()
	{
		// there should be nothing here ig
	}
}
