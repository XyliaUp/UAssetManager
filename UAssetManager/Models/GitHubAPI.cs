using System.Net.Http;
using System.Reflection;
using System.Windows;
using UAssetManager.Resources;
using UAssetManager.Utils;

namespace UAssetManager.Models;
internal static class GitHubAPI
{
	private static readonly string GitHubRepo = "XyliaUp/UAssetManager";

	private static string CombineURI(params string[] uris)
	{
		string output = "";
		foreach (string uriBit in uris)
		{
			output += uriBit.Trim('/') + "/";
		}
		return output.TrimEnd('/');
	}

	private static string GetLatestVersionURL(string repo)
	{
		return CombineURI("https://github.com", repo, "releases", "latest");
	}

	private static async Task<Version?> GetLatestVersionAsync(string repo)
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

	/// <summary>
	/// Fetch the latest version from github
	/// </summary>
	public static async Task CheckForUpdates()
	{
		if (!UAGConfig.Data.EnableUpdateNotice) return;

		var latestVersion = await GetLatestVersionAsync(GitHubRepo);
		var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;

		if (latestVersion != null && latestVersion > currentVersion)
		{
			var dialog = MessageBox.Show(
				StringHelper.Get("Dialog.NewVersionAvailable", latestVersion),
				StringHelper.Get("MainWindow_Title"),
				MessageBoxButton.YesNo, MessageBoxImage.Information);
			if (dialog == MessageBoxResult.Yes) UAGUtils.OpenUrl(GetLatestVersionURL(GitHubRepo));
		}
	}
}