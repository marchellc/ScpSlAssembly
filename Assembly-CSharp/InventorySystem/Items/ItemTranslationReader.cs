using System;
using UnityEngine;

namespace InventorySystem.Items
{
	public static class ItemTranslationReader
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			TranslationReader.OnTranslationsRefreshed += ItemTranslationReader.ReloadTranslations;
		}

		private static void ReloadTranslations()
		{
			if (ItemTranslationReader._cachedTranslations == null)
			{
				ItemTranslationReader._cachedTranslations = new ItemTranslationReader.NameDescriptionPair[InventoryItemLoader.AvailableItems.Count];
			}
			int num = 0;
			string text;
			while (TranslationReader.TryGet("Items", num, out text))
			{
				string[] array = text.Split('~', StringSplitOptions.None);
				int num2;
				if (array.Length >= 2 && int.TryParse(array[0], out num2))
				{
					string text2 = array[1];
					string text3 = ((array.Length > 2) ? array[2] : string.Empty);
					ItemTranslationReader.EnsureCapacity(num2);
					ItemTranslationReader._cachedTranslations[num2] = new ItemTranslationReader.NameDescriptionPair
					{
						Name = text2,
						Description = text3
					};
				}
				num++;
			}
		}

		private static void EnsureCapacity(int cap)
		{
			if (cap < ItemTranslationReader._cachedTranslations.Length)
			{
				return;
			}
			Array.Resize<ItemTranslationReader.NameDescriptionPair>(ref ItemTranslationReader._cachedTranslations, cap * 2);
		}

		private static ItemTranslationReader.NameDescriptionPair GetTranslation(ItemType it)
		{
			int num = (int)it;
			if (num < 0)
			{
				throw new InvalidOperationException(string.Format("Cannot load translation for item {0}", it));
			}
			if (ItemTranslationReader._cachedTranslations == null)
			{
				ItemTranslationReader.ReloadTranslations();
			}
			ItemTranslationReader.NameDescriptionPair nameDescriptionPair = ItemTranslationReader._cachedTranslations[num];
			if (nameDescriptionPair == null)
			{
				nameDescriptionPair = new ItemTranslationReader.NameDescriptionPair
				{
					Name = it.ToString()
				};
				ItemTranslationReader._cachedTranslations[num] = nameDescriptionPair;
			}
			return nameDescriptionPair;
		}

		public static string GetName(this ItemType it)
		{
			return ItemTranslationReader.GetTranslation(it).Name;
		}

		public static string GetDescription(this ItemType it)
		{
			return ItemTranslationReader.GetTranslation(it).Description;
		}

		private const char SplitChar = '~';

		private static ItemTranslationReader.NameDescriptionPair[] _cachedTranslations;

		private class NameDescriptionPair
		{
			public string Name;

			public string Description;
		}
	}
}
