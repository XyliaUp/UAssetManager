using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows;
using UAssetManager.Models;

namespace UAssetManager.Resources;

/// <summary>
/// Text Controller
/// </summary>
public class StringHelper : ResourceDictionary
{
	#region Constructor
	public static StringHelper? Current { get; private set; }

	public StringHelper()
	{
		Current = this;
        Language = UAGConfig.Data.Language;
    }
	#endregion

	#region CultureInfo
	private CultureInfo CultureInfo = CultureInfo.CurrentCulture;

	protected virtual string BasePath => "UAssetManager.Resources.Strings.Strings";

    public ELanguage Language
    {
        get => EnumerateLanguages().FirstOrDefault(x => GetLanguageCode(x) == CultureInfo.Name);
        set
        {
            // culture
            var languageCode = GetLanguageCode(value);
            if (!string.IsNullOrEmpty(languageCode))
            {
                CultureInfo = new CultureInfo(languageCode);
            }

            // resource
            var manager = new ResourceManager(BasePath, Assembly.GetEntryAssembly()!);
            foreach (DictionaryEntry entry in manager.GetResourceSet(CultureInfo, true, true)!)
            {
                var resourceKey = entry.Key.ToString()!;
                var resource = entry.Value;

                this[resourceKey] = resource;
            }
        }
    }
	#endregion

	#region Static Methods
	internal static IEnumerable<ELanguage> EnumerateLanguages() => Enum.GetValues<ELanguage>();

	/// <summary>
	/// Get language code from language enum
	/// </summary>
	/// <param name="language">Language enum</param>
	/// <returns>Language code</returns>
	private static string GetLanguageCode(ELanguage language)
	{
		return language switch
		{
			ELanguage.ChineseS => "zh-CN",
			_ => "en-US",
		};
	}

	/// <summary>
	/// Gets text and replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
	/// </summary>
	/// <param name="key">Target text resource key</param>
	/// <param name="args">An object array that contains zero or more objects to format.</param>
	/// <returns></returns>
	public static string Get(string key, params object?[] args)
	{
		if (string.IsNullOrEmpty(key)) return string.Empty;
		if (Current?[key] is string s) return string.Format(s, args);

		return key;
	}
	#endregion
}

public enum ELanguage
{
    [Description("English")]
    English,

    [Description("中文简体 (Chinese)")]
    ChineseS
}