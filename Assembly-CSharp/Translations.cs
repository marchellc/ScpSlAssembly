using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.NonAllocLINQ;

public static class Translations
{
	private static void DefineKey(string key)
	{
		string formatted = key.Replace("_", string.Empty).ToLowerInvariant();
		Translations.TypeToFilename[formatted] = key;
		Translations.Suffixes.ForEach(delegate(string x)
		{
			Translations.TypeToFilename[formatted + x] = key;
		});
	}

	private static bool TryGenerate(Type enumType, out Translations.LoadedTranslation[] generated)
	{
		if (!Translations._pascalCaseGenerated)
		{
			Translations._pascalCaseGenerated = true;
			TranslationReader.Fallback.ForEachKey(new Action<string>(Translations.DefineKey));
		}
		string text;
		if (!Translations.TypeToFilename.TryGetValue(enumType.Name.ToLowerInvariant(), out text))
		{
			generated = new Translations.LoadedTranslation[0];
			return false;
		}
		string[] array = TranslationReader.Fallback[text];
		string[] array2;
		bool flag = TranslationReader.Elements.TryGetValue(text, out array2);
		int num = array.Length;
		generated = new Translations.LoadedTranslation[num];
		for (int i = 0; i < num; i++)
		{
			generated[i] = new Translations.LoadedTranslation(array, array2, flag, i);
		}
		Translations.AllTranslations[enumType] = generated;
		return true;
	}

	public static bool TryGet(Type type, int index, out string str)
	{
		Translations.LoadedTranslation[] array;
		if (!Translations.AllTranslations.TryGetValue(type, out array) && !Translations.TryGenerate(type, out array))
		{
			str = null;
			return false;
		}
		if (index >= array.Length)
		{
			str = null;
			return false;
		}
		Translations.LoadedTranslation loadedTranslation = array[index];
		str = (loadedTranslation.HasLocal ? loadedTranslation.Localized : loadedTranslation.Fallback);
		return !string.IsNullOrWhiteSpace(str);
	}

	public static void ResetCache()
	{
		Translations.AllTranslations.Clear();
	}

	public static bool TryGet<T>(T val, out string tr) where T : Enum
	{
		Type typeFromHandle = typeof(T);
		int num = val.ToInt32(null);
		if (Translations.TryGet(typeFromHandle, num, out tr))
		{
			return true;
		}
		tr = val.ToString();
		Debug.LogWarning(string.Concat(new string[] { "Missing translation! ", typeFromHandle.Name, ":", tr, "." }));
		return false;
	}

	public static string Get<T>(T val) where T : Enum
	{
		string text;
		Translations.TryGet<T>(val, out text);
		return text;
	}

	public static string Get<T>(T val, string fallback) where T : Enum
	{
		string text;
		if (!Translations.TryGet<T>(val, out text))
		{
			return fallback;
		}
		return text;
	}

	private static bool _pascalCaseGenerated;

	private static readonly Dictionary<Type, Translations.LoadedTranslation[]> AllTranslations = new Dictionary<Type, Translations.LoadedTranslation[]>();

	private static readonly Dictionary<string, string> TypeToFilename = new Dictionary<string, string>();

	private static readonly string[] Suffixes = new string[] { "translation", "key" };

	private class LoadedTranslation
	{
		public LoadedTranslation(string[] fallbacks, string[] locals, bool hasLocal, int i)
		{
			this.Fallback = fallbacks[i];
			if (hasLocal && i < locals.Length)
			{
				this.Localized = locals[i];
				this.HasLocal = !string.IsNullOrWhiteSpace(this.Localized);
				return;
			}
			this.Localized = null;
			this.HasLocal = false;
		}

		public readonly string Localized;

		public readonly string Fallback;

		public readonly bool HasLocal;
	}
}
