using System.Net.Http;
using System.Reflection;
using System.Windows;
using UAssetManager.Resources;
using UAssetManager.Utils;

namespace UAssetManager.Models;
public static class GitHubAPI
{
    public static readonly string GitHubRepo = "XyliaUp/UAssetManager";

    public static string CombineURI(params string[] uris)
    {
        string output = "";
        foreach (string uriBit in uris)
        {
            output += uriBit.Trim('/') + "/";
        }
        return output.TrimEnd('/');
    }

    public static string GetLatestVersionURL(string repo)
    {
        return CombineURI("https://github.com", repo, "releases", "latest");
    }

    public static async Task<Version?> GetLatestVersionAsync(string repo)
    {
        try
        {
            var handler = new HttpClientHandler() { AllowAutoRedirect = false };
            var client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Get, GetLatestVersionURL(repo));
            request.Headers.Add("User-Agent", "UAssetGUI");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            var newURL = response.Headers.Location?.ToString();
            if (string.IsNullOrEmpty(newURL)) return null;

            string[] splitURL = newURL.Split('/');
            string finalVersionBit = splitURL[^1];

            if (finalVersionBit.StartsWith("v"))
                finalVersionBit = finalVersionBit.Substring(1);

            finalVersionBit = finalVersionBit.Replace(".0-alpha.", ".");
            return Version.Parse(finalVersionBit);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static async Task CheckForUpdates()
    {
        if (!UAGConfig.Data.EnableUpdateNotice) return;

        var latestVersion = await GetLatestVersionAsync(GitHubRepo);
        var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;

        if (latestVersion != null && latestVersion > currentVersion)
        {
            var dialog = MessageBox.Show(
                StringHelper.Get("Dialog.NewVersionAvailable"),
                StringHelper.Get("MainWindow_Title"), MessageBoxButton.YesNo);
            if (dialog == MessageBoxResult.Yes) UAGUtils.OpenUrl(GetLatestVersionURL(GitHubRepo));
        }
    }
}