namespace UAssetManager.Pak.Versions;
public enum EUnrealEngineObjectUE4Version
{
    AUTOMATIC_VERSION = 0
}

public enum EUnrealEngineObjectUE5Version
{
    AUTOMATIC_VERSION = 0
}

public readonly struct FPackageFileVersion
{
    public readonly int FileVersionUE4;
    public readonly int FileVersionUE5;

    public FPackageFileVersion(int fileVersionUe4, int fileVersionUe5)
    {
        FileVersionUE4 = fileVersionUe4;
        FileVersionUE5 = fileVersionUe5;
    }

    public static FPackageFileVersion CreateUE4Version(int version) => new(version, 0);
}