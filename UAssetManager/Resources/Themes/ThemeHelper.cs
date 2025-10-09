using UAssetManager.Models;

namespace UAssetManager.Resources.Themes;
internal class ThemeHelper
{
    public static ThemeType CurrentTheme { get; private set; } = UAGConfig.Data.Theme;
}

public enum ThemeType
{
    Light,
    Dark,
    System
}