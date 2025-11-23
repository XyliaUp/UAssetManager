using System.Diagnostics;

namespace UAssetManager.Utils;
public static class UAGUtils
{   
    public static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fail to open url: {ex.Message}");
        }
    }

    public static void OpenFolder(string folderPath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fail to open folder: {ex.Message}");
        }
    }
}