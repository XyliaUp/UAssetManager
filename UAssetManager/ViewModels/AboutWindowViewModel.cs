using System.IO;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UAssetManager.Views;

namespace UAssetManager.ViewModels;

internal partial class AboutWindowViewModel : ObservableObject
{
	#region Properties
	public string Version => Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
	#endregion

	#region Methods

	[RelayCommand]
	void ViewLicense()
	{
		try
		{
			// Try to read license file
			string licenseText = GetLicenseText();

			if (!string.IsNullOrEmpty(licenseText))
			{
				var licenseWindow = new MarkdownViewerWindow
				{
					Title = "License",
					MarkdownContent = licenseText
				};
				licenseWindow.ShowDialog();
			}
			else
			{
				MessageBox.Show("Unable to find license file", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error displaying license: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}

	[RelayCommand]
	void ViewNotice()
	{
		try
		{
			// Try to read notice file
			string noticeText = GetNoticeText();

			if (!string.IsNullOrEmpty(noticeText))
			{
				var noticeWindow = new MarkdownViewerWindow
				{
					Title = "Third-party Software",
					MarkdownContent = noticeText
				};
				noticeWindow.ShowDialog();
			}
			else
			{
				MessageBox.Show("Unable to find notice file", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Error displaying notice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
		}
	}


	private string GetLicenseText()
	{
		try
		{
			// Try to read license from embedded resources
			var assembly = Assembly.GetExecutingAssembly();
			using var stream = assembly.GetManifestResourceStream("UAssetManager.LICENSE");

			if (stream != null)
			{
				using var reader = new StreamReader(stream);
				return reader.ReadToEnd();
			}

			// If embedded resource doesn't exist, try to read from file system
			var licensePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LICENSE");
			if (File.Exists(licensePath))
			{
				return File.ReadAllText(licensePath);
			}

			// Return default license text
			return GetDefaultLicenseText();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error reading license: {ex.Message}");
			return GetDefaultLicenseText();
		}
	}

	private string GetNoticeText()
	{
		try
		{
			// Try to read notice from embedded resources
			var assembly = Assembly.GetExecutingAssembly();
			using var stream = assembly.GetManifestResourceStream("UAssetManager.NOTICE.md");

			if (stream != null)
			{
				using var reader = new StreamReader(stream);
				return reader.ReadToEnd();
			}

			// If embedded resource doesn't exist, try to read from file system
			var noticePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "NOTICE.md");
			if (File.Exists(noticePath))
			{
				return File.ReadAllText(noticePath);
			}

			// Return default notice text
			return GetDefaultNoticeText();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error reading notice: {ex.Message}");
			return GetDefaultNoticeText();
		}
	}

	private string GetDefaultNoticeText()
	{
		return @"# Third-party Software

This software uses the following third-party libraries:

## UAssetAPI
- **License**: MIT License
- **Description**: Library for reading and writing Unreal Engine asset files
- **Website**: https://github.com/atenfyr/UAssetAPI

## Newtonsoft.Json
- **License**: MIT License
- **Description**: Popular high-performance JSON framework for .NET
- **Website**: https://www.newtonsoft.com/json

## DiscordRichPresence
- **License**: MIT License
- **Description**: Discord Rich Presence integration for .NET
- **Website**: https://github.com/Lachee/discord-rpc-csharp

## Microsoft.Xaml.Behaviors.Wpf
- **License**: MIT License
- **Description**: Behaviors SDK for WPF
- **Website**: https://github.com/microsoft/XamlBehaviorsWpf

## WPF Toolkit Extended
- **License**: Microsoft Public License (Ms-PL)
- **Description**: Extended WPF Toolkit with additional controls
- **Website**: https://github.com/xceedsoftware/wpftoolkit

All third-party libraries are used in accordance with their respective licenses.";
	}

	private string GetDefaultLicenseText()
	{
		return @"# UAssetManager License

## MIT License

Copyright (c) 2024 UAssetManager

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Third-party Libraries

This software uses the following third-party libraries:

- UAssetAPI - MIT License
- Newtonsoft.Json - MIT License
- DiscordRichPresence - MIT License
- Microsoft.Xaml.Behaviors.Wpf - MIT License";
	}

	#endregion
}