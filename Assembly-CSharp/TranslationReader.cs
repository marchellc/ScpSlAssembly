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

	public static event Action OnTranslationsRefreshed;

	public static bool EverLoaded { get; private set; }

	public static string TranslationDirectoryName { get; private set; }

	public static TranslationManifest TranslationManifest { get; private set; }

	public static CultureInfo TranslationCulture { get; private set; }

	static TranslationReader()
	{
		TranslationReader.LoadPositions();
		SceneManager.sceneLoaded += TranslationReader.OnSceneWasLoaded;
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
		TranslationReader.LoadTranslation(TranslationReader.CheckPath(Array.Empty<string>()), TranslationReader.Fallback);
		TranslationReader.TranslationCulture = null;
		foreach (string text in TranslationReader.TranslationManifest.InterfaceLocales ?? Array.Empty<string>())
		{
			try
			{
				TranslationReader.TranslationCulture = CultureInfo.GetCultureInfo(text);
				break;
			}
			catch
			{
				TranslationReader.TranslationCulture = null;
			}
		}
		if (TranslationReader.TranslationCulture == null)
		{
			TranslationReader.TranslationCulture = CultureInfo.CurrentCulture;
		}
		CultureInfo.CurrentCulture = TranslationReader.TranslationCulture;
		CultureInfo.CurrentUICulture = TranslationReader.TranslationCulture;
		TranslationReader.EverLoaded = true;
		Translations.ResetCache();
		Action onTranslationsRefreshed = TranslationReader.OnTranslationsRefreshed;
		if (onTranslationsRefreshed == null)
		{
			return;
		}
		onTranslationsRefreshed();
	}

	private static string GetTranslationPath()
	{
		string text = TranslationReader.CheckPath(Array.Empty<string>());
		if (text == null)
		{
			throw new DirectoryNotFoundException();
		}
		return text;
	}

	private static void LoadPositions()
	{
		foreach (string text in Directory.GetFiles(TranslationReader.CheckPath(Array.Empty<string>())))
		{
			string[] array = File.ReadAllLines(text);
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
			TranslationReader._positions.Add(Path.GetFileNameWithoutExtension(text), dictionary);
		}
	}

	private static TranslationManifest LoadTranslation(string translationPath, Dictionary<string, string[]> dictionary)
	{
		dictionary.Clear();
		if (File.Exists(translationPath + "Legacy_Interfaces.txt") && File.Exists(translationPath + "Legancy_Interfaces.txt"))
		{
			File.Delete(translationPath + "Legancy_Interfaces.txt");
		}
		foreach (string text in Directory.EnumerateFiles(translationPath, "*.txt"))
		{
			string[] array = FileManager.ReadAllLines(text);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Replace("\\n", Environment.NewLine);
				foreach (object obj in TranslationReader._matchFormat.Matches(array[i]))
				{
					Match match = (Match)obj;
					Dictionary<int, Dictionary<string, int>> dictionary2;
					Dictionary<string, int> dictionary3;
					int num;
					if (TranslationReader._positions.TryGetValue(Path.GetFileNameWithoutExtension(text), out dictionary2) && dictionary2.TryGetValue(i, out dictionary3) && dictionary3.TryGetValue(match.Value, out num))
					{
						array[i] = array[i].Replace(match.Value, "{" + num.ToString() + "}");
					}
				}
			}
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
			dictionary[(fileNameWithoutExtension == "Legancy_Interfaces") ? "Legacy_Interfaces" : fileNameWithoutExtension] = array;
		}
		TranslationManifest translationManifest;
		try
		{
			translationManifest = JsonSerialize.FromFile<TranslationManifest>(Path.Combine(translationPath, "manifest.json"));
		}
		catch (FileNotFoundException)
		{
			translationManifest = new TranslationManifest(Path.GetFileName(translationPath), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
		}
		return translationManifest;
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
		return Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any<string>();
	}

	public static string[] GetKeys(string keyName)
	{
		if (keyName == "Legancy_Interfaces")
		{
			keyName = "Legacy_Interfaces";
		}
		string[] array;
		if (TranslationReader.Fallback.TryGetValue(keyName, out array))
		{
			return array;
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
		string[] array;
		if (!TranslationReader.Fallback.TryGetValue(keyName, out array))
		{
			return null;
		}
		return array;
	}

	public static string Get(string keyName, int index, string defaultValue = "NO_TRANSLATION")
	{
		if (global::GameCore.Console.TranslationDebugMode)
		{
			return string.Format("{0}:{1}", keyName, index);
		}
		if (keyName == "Legancy_Interfaces")
		{
			keyName = "Legacy_Interfaces";
		}
		return TranslationReader.GetFallback(keyName, index, defaultValue) ?? defaultValue;
	}

	public static bool TryGet(string keyName, int index, out string val)
	{
		string[] array;
		string text;
		if (TranslationReader.Fallback.TryGetValue(keyName, out array) && array.TryGet(index, out text) && !string.IsNullOrWhiteSpace(text))
		{
			val = text;
			return true;
		}
		val = TranslationReader.GetFallback(keyName, index, "NO_TRANSLATION");
		return false;
	}

	private static string GetFallback(string keyName, int index, string defaultvalue)
	{
		string[] array;
		string text;
		if (TranslationReader.Fallback.TryGetValue(keyName, out array) && array.TryGet(index, out text) && !string.IsNullOrWhiteSpace(text))
		{
			return text;
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

	private static string _translationPath;

	public static readonly Dictionary<string, string[]> Elements = new Dictionary<string, string[]>();

	public static readonly Dictionary<string, string[]> Fallback = new Dictionary<string, string[]>();

	private static readonly Dictionary<string, Dictionary<int, Dictionary<string, int>>> _positions = new Dictionary<string, Dictionary<int, Dictionary<string, int>>>();

	private static readonly Regex _matchFormat = new Regex("\\{.*?\\}|\\[.*?\\]");

	private static TMP_FontAsset[] defaultFallbacks;

	public const string DefaultLanguage = "en";

	public const string NoTranslation = "NO_TRANSLATION";

	public const string TranslationDirectory = "Translations/";
}
