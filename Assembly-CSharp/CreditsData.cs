using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CreditsData
{
	static CreditsData()
	{
		try
		{
			CreditsData.SetCredits(File.ReadAllText(CreditsData.CachePath));
		}
		catch
		{
			CreditsData.LoadData(File.ReadAllText("CreditsCache.json"));
		}
	}

	internal static void LoadData(string text)
	{
		try
		{
			CreditsData.SetCredits(text);
			File.WriteAllText(CreditsData.CachePath, text);
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	private static void SetCredits(string text)
	{
		if (!text.StartsWith('{'))
		{
			throw new ArgumentException("Invalid credits data", "text");
		}
		CreditsList creditsList = JsonSerialize.FromJson<CreditsList>(text);
		List<CreditsCategory> list = new List<CreditsCategory>();
		TranslationManifest[] array = (from s in Directory.EnumerateFiles("Translations/", "manifest.json", SearchOption.AllDirectories)
			select JsonSerialize.FromFile<TranslationManifest>(s)).ToArray<TranslationManifest>();
		for (int i = 0; i < creditsList.credits.Length; i++)
		{
			CreditsListCategory creditsListCategory = creditsList.credits[i];
			List<CreditsEntry> list2 = new List<CreditsEntry>();
			for (int j = 0; j < creditsListCategory.members.Length; j++)
			{
				list2.Add(CreditsData.ProcessEntry(creditsListCategory.members[j]));
			}
			if (creditsListCategory.category.Equals("SPECIAL THANKS", StringComparison.OrdinalIgnoreCase))
			{
				list2.Add(new CreditsEntry(TranslationReader.Get("NewMainMenu", 87, "For playing our game!"), "<current nickname>", new Color32(byte.MaxValue, 215, 0, byte.MaxValue)));
				list.Add(new CreditsCategory
				{
					Header = creditsListCategory.category,
					Records = list2.ToArray()
				});
				list2.Clear();
				foreach (TranslationManifest translationManifest in array)
				{
					string text2 = translationManifest.Name;
					if (!text2.Equals("en", StringComparison.OrdinalIgnoreCase))
					{
						int num = translationManifest.Name.IndexOf('-');
						if (num > -1)
						{
							text2 = translationManifest.Name.Substring(num + 2);
						}
						foreach (string text3 in translationManifest.Authors)
						{
							list2.Add(new CreditsEntry(text2, text3));
						}
						list2.Add(new CreditsEntry("", ""));
					}
				}
				list.Add(new CreditsCategory
				{
					Header = "COMMUNITY TRANSLATORS",
					Records = list2.ToArray()
				});
			}
			else
			{
				list.Add(new CreditsCategory
				{
					Header = creditsListCategory.category,
					Records = list2.ToArray()
				});
			}
		}
		CreditsData.Data = list.ToArray();
		CreditsData.RemoteLoaded = true;
	}

	private static CreditsEntry ProcessEntry(CreditsListMember member)
	{
		if (string.IsNullOrEmpty(member.name))
		{
			return new CreditsEntry();
		}
		if (!string.IsNullOrEmpty(member.title))
		{
			return new CreditsEntry(member.title, member.name, CreditsData.HexToColor(member));
		}
		return new CreditsEntry(member.name);
	}

	private static Color32 HexToColor(CreditsListMember member)
	{
		if (string.IsNullOrEmpty(member.color))
		{
			return Color.white;
		}
		Color32 color;
		if (Misc.TryParseColor(member.color, out color))
		{
			return color;
		}
		Debug.LogError(string.Concat(new string[] { "Error during processing credits color (", member.name, " - ", member.title, " - ", member.color, ")." }));
		return Color.white;
	}

	public static bool RemoteLoaded;

	internal const string CurrentNicknamePlaceholder = "<current nickname>";

	private static readonly string CachePath = FileManager.GetAppFolder(true, false, "") + "CreditsCache.json";

	internal static CreditsCategory[] Data;
}
