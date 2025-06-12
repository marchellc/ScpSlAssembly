using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GameCore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TranslationReader
{
	private static string _translationPath;

	public static readonly Dictionary<string, string[]> Elements;

	public static readonly Dictionary<string, string[]> Fallback;

	private static readonly Dictionary<string, Dictionary<int, Dictionary<string, int>>> _positions;

	private static readonly Regex _matchFormat;

	private static TMP_FontAsset[] defaultFallbacks;

	public const string DefaultLanguage = "en";

	public const string NoTranslation = "NO_TRANSLATION";

	public const string TranslationDirectory = "Translations/";

	public static string TranslationPath
	{
		get
		{
			return TranslationReader._translationPath;
		}
		private set
		{
			TranslationReader._translationPath = Path.GetFullPath(value);
			TranslationReader.TranslationDirectoryName = Path.GetFileName(TranslationReader._translationPath);
		}
	}

	public static bool EverLoaded { get; private set; }

	public static string TranslationDirectoryName { get; private set; }

	public static TranslationManifest TranslationManifest { get; private set; }

	public static CultureInfo TranslationCulture { get; private set; }

	public static event Action OnTranslationsRefreshed;

	static TranslationReader()
	{
		TranslationReader.Elements = new Dictionary<string, string[]>();
		TranslationReader.Fallback = new Dictionary<string, string[]>();
		TranslationReader._positions = new Dictionary<string, Dictionary<int, Dictionary<string, int>>>();
		TranslationReader._matchFormat = new Regex("\\{.*?\\}|\\[.*?\\]");
		TranslationReader.LoadPositions();
		SceneManager.sceneLoaded += OnSceneWasLoaded;
	}

	private static void OnSceneWasLoaded(Scene scene, LoadSceneMode mode)
	{
		PlayerPrefsSl.Refresh();
		TranslationReader.Refresh();
	}

	public static void Refresh()
	{
		TranslationReader.TranslationPath = TranslationReader.GetTranslationPath();
		TranslationReader.TranslationManifest = TranslationReader.LoadTranslation(TranslationReader.TranslationPath, TranslationReader.Elements);
		TranslationReader.LoadTranslation(TranslationReader.CheckPath(), TranslationReader.Fallback);
		TranslationReader.TranslationCulture = null;
		string[] array = TranslationReader.TranslationManifest.InterfaceLocales ?? Array.Empty<string>();
		foreach (string name in array)
		{
			try
			{
				TranslationReader.TranslationCulture = CultureInfo.GetCultureInfo(name);
			}
			catch
			{
				TranslationReader.TranslationCulture = null;
				continue;
			}
			break;
		}
		if (TranslationReader.TranslationCulture == null)
		{
			TranslationReader.TranslationCulture = CultureInfo.CurrentCulture;
		}
		CultureInfo.CurrentCulture = TranslationReader.TranslationCulture;
		CultureInfo.CurrentUICulture = TranslationReader.TranslationCulture;
		TranslationReader.EverLoaded = true;
		Translations.ResetCache();
		TranslationReader.OnTranslationsRefreshed?.Invoke();
	}

	private static string GetTranslationPath()
	{
		return TranslationReader.CheckPath() ?? throw new DirectoryNotFoundException();
	}

	private static void LoadPositions()
	{
		string[] files = Directory.GetFiles(TranslationReader.CheckPath());
		foreach (string path in files)
		{
			string[] array = File.ReadAllLines(path);
			Dictionary<int, Dictionary<string, int>> dictionary = new Dictionary<int, Dictionary<string, int>>();
			for (int j = 0; j < array.Length; j++)
			{
				Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
				MatchCollection matchCollection = TranslationReader._matchFormat.Matches(array[j]);
				for (int k = 0; k < matchCollection.Count; k++)
				{
					dictionary2.TryAdd(matchCollection[k].Value, k);
				}
				dictionary.Add(j, dictionary2);
			}
			TranslationReader._positions.Add(Path.GetFileNameWithoutExtension(path), dictionary);
		}
	}

	private static TranslationManifest LoadTranslation(string translationPath, Dictionary<string, string[]> dictionary)
	{
		dictionary.Clear();
		if (File.Exists(translationPath + "Legacy_Interfaces.txt") && File.Exists(translationPath + "Legancy_Interfaces.txt"))
		{
			File.Delete(translationPath + "Legancy_Interfaces.txt");
		}
		foreach (string item in Directory.EnumerateFiles(translationPath, "*.txt"))
		{
			string[] array = FileManager.ReadAllLines(item);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Replace("\\n", Environment.NewLine);
				foreach (Match item2 in TranslationReader._matchFormat.Matches(array[i]))
				{
					if (TranslationReader._positions.TryGetValue(Path.GetFileNameWithoutExtension(item), out var value) && value.TryGetValue(i, out var value2) && value2.TryGetValue(item2.Value, out var value3))
					{
						array[i] = array[i].Replace(item2.Value, "{" + value3 + "}");
					}
				}
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
			dictionary[(fileNameWithoutExtension == "Legancy_Interfaces") ? "Legacy_Interfaces" : fileNameWithoutExtension] = array;
		}
		try
		{
			return JsonSerialize.FromFile<TranslationManifest>(Path.Combine(translationPath, "manifest.json"));
		}
		catch (FileNotFoundException)
		{
			return new TranslationManifest(Path.GetFileName(translationPath), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
		}
	}

	private static string CheckPath(params string[] suffixes)
	{
		if (suffixes == null || suffixes.Length == 0)
		{
			return TranslationReader.CheckDefaultLangPath();
		}
		foreach (string text in suffixes)
		{
			if (!string.IsNullOrWhiteSpace(text))
			{
				string text2 = Path.Combine("Translations/", text);
				if (TranslationReader.CheckPathInternal(text2))
				{
					return text2;
				}
			}
		}
		return TranslationReader.CheckDefaultLangPath();
	}

	private static string CheckDefaultLangPath()
	{
		string text = Path.Combine("Translations/", "en");
		if (!TranslationReader.CheckPathInternal(text))
		{
			return null;
		}
		return text;
	}

	private static bool CheckPathInternal(string path)
	{
		if (Directory.Exists(path))
		{
			return Directory.EnumerateFileSystemEntries(path).Any();
		}
		return false;
	}

	public static string[] GetKeys(string keyName)
	{
		if (keyName == "Legancy_Interfaces")
		{
			keyName = "Legacy_Interfaces";
		}
		if (TranslationReader.Fallback.TryGetValue(keyName, out var value))
		{
			return value;
		}
		Debug.LogWarning("Tried to get **FALLBACK** translation from nonexistent file " + keyName);
		return null;
	}

	public static string[] GetFallbackKeys(string keyName)
	{
		if (keyName == "Legancy_Interfaces")
		{
			keyName = "Legacy_Interfaces";
		}
		if (!TranslationReader.Fallback.TryGetValue(keyName, out var value))
		{
			return null;
		}
		return value;
	}

	public static string Get(string keyName, int index, string defaultValue = "NO_TRANSLATION")
	{
		if (GameCore.Console.TranslationDebugMode)
		{
			return $"{keyName}:{index}";
		}
		if (keyName == "Legancy_Interfaces")
		{
			keyName = "Legacy_Interfaces";
		}
		return TranslationReader.GetFallback(keyName, index, defaultValue) ?? defaultValue;
	}

	public static bool TryGet(string keyName, int index, out string val)
	{
		if (TranslationReader.Fallback.TryGetValue(keyName, out var value) && value.TryGet(index, out var element) && !string.IsNullOrWhiteSpace(element))
		{
			val = element;
			return true;
		}
		val = TranslationReader.GetFallback(keyName, index, "NO_TRANSLATION");
		return false;
	}

	private static string GetFallback(string keyName, int index, string defaultvalue)
	{
		if (TranslationReader.Fallback.TryGetValue(keyName, out var value) && value.TryGet(index, out var element) && !string.IsNullOrWhiteSpace(element))
		{
			return element;
		}
		Debug.LogWarning(string.Format("Missing **FALLBACK** translation! {0}:{1}. Default value: {2}", keyName, index, defaultvalue.Replace("<", "(<)")));
		return null;
	}

	public static string GetFormatted(string keyName, int index, string defaultvalue, object obj1)
	{
		return string.Format(TranslationReader.Get(keyName, index, defaultvalue), obj1);
	}

	public static string GetFormatted(string keyName, int index, string defaultvalue, object obj1, object obj2)
	{
		return string.Format(TranslationReader.Get(keyName, index, defaultvalue), obj1, obj2);
	}

	public static string GetFormatted(string keyName, int index, string defaultvalue, object obj1, object obj2, object obj3)
	{
		return string.Format(TranslationReader.Get(keyName, index, defaultvalue), obj1, obj2, obj3);
	}

	public static string GetFormatted(string keyName, int index, string defaultvalue, params object[] format)
	{
		return string.Format(TranslationReader.Get(keyName, index, defaultvalue), format);
	}
}
