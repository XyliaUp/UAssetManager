namespace UAssetManager.Pak.Versions;
public class VersionContainer
{
    public EGame Game { get; set; } = EGame.GAME_UE4_LATEST;
    public FPackageFileVersion Ver { get; set; }
}