using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.NonAllocLINQ;

public static class Translations
{
	private class LoadedTranslation
	{
		public readonly string Localized;

		public readonly string Fallback;

		public readonly bool HasLocal;

		public LoadedTranslation(string[] fallbacks, string[] locals, bool hasLocal, int i)
		{
			this.Fallback = fallbacks[i];
			if (hasLocal && i < locals.Length)
			{
				this.Localized = locals[i];
				this.HasLocal = !string.IsNullOrWhiteSpace(this.Localized);
			}
			else
			{
				this.Localized = null;
				this.HasLocal = false;
			}
		}
	}

	private static bool _pascalCaseGenerated;

	private static readonly Dictionary<Type, LoadedTranslation[]> AllTranslations = new Dictionary<Type, LoadedTranslation[]>();

	private static readonly Dictionary<string, string> TypeToFilename = new Dictionary<string, string>();

	private static readonly string[] Suffixes = new string[2] { "translation", "key" };

	private static void DefineKey(string key)
	{
		string formatted = key.Replace("_", string.Empty).ToLowerInvariant();
		Translations.TypeToFilename[formatted] = key;
		Translations.Suffixes.ForEach(delegate(string x)
		{
			Translations.TypeToFilename[formatted + x] = key;
		});
	}

	private static bool TryGenerate(Type enumType, out LoadedTranslation[] generated)
	{
		if (!Translations._pascalCaseGenerated)
		{
			Translations._pascalCaseGenerated = true;
			TranslationReader.Fallback.ForEachKey(DefineKey);
		}
		if (!Translations.TypeToFilename.TryGetValue(enumType.Name.ToLowerInvariant(), out var value))
		{
			generated = new LoadedTranslation[0];
			return false;
		}
		string[] array = TranslationReader.Fallback[value];
		string[] value2;
		bool hasLocal = TranslationReader.Elements.TryGetValue(value, out value2);
		int num = array.Length;
		generated = new LoadedTranslation[num];
		for (int i = 0; i < num; i++)
		{
			generated[i] = new LoadedTranslation(array, value2, hasLocal, i);
		}
		Translations.AllTranslations[enumType] = generated;
		return true;
	}

	public static bool TryGet(Type type, int index, out string str)
	{
		if (!Translations.AllTranslations.TryGetValue(type, out var value) && !Translations.TryGenerate(type, out value))
		{
			str = null;
			return false;
		}
		if (index >= value.Length)
		{
			str = null;
			return false;
		}
		LoadedTranslation loadedTranslation = value[index];
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
		int index = ((IConvertible)val).ToInt32((IFormatProvider)null);
		if (Translations.TryGet(typeFromHandle, index, out tr))
		{
			return true;
		}
		tr = val.ToString();
		Debug.LogWarning("Missing translation! " + typeFromHandle.Name + ":" + tr + ".");
		return false;
	}

	public static string Get<T>(T val) where T : Enum
	{
		Translations.TryGet(val, out var tr);
		return tr;
	}

	public static string Get<T>(T val, string fallback) where T : Enum
	{
		if (!Translations.TryGet(val, out var tr))
		{
			return fallback;
		}
		return tr;
	}
}
