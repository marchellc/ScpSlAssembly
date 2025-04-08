using System;

namespace InventorySystem.Items.Usables.Scp330
{
	public static class Scp330Translations
	{
		public static string GetSpecificTranslation(int index, string fallback)
		{
			return TranslationReader.Get("SCP330", index, fallback);
		}

		public static string GetEntryTranslation(Scp330Translations.Entry entry)
		{
			return Scp330Translations.GetSpecificTranslation((int)entry, entry.ToString());
		}

		public static void GetCandyTranslation(CandyKindID candyKind, out string name, out string desc, out string fx)
		{
			int num = (int)(1 + candyKind * CandyKindID.Purple);
			name = Scp330Translations.GetSpecificTranslation(num - 3, candyKind.ToString());
			desc = Scp330Translations.GetSpecificTranslation(num - 2, "No Description");
			fx = Scp330Translations.GetSpecificTranslation(num - 1, "Unknown effects");
		}

		private const int CandiesOffset = 1;

		public enum Entry
		{
			Candies
		}
	}
}
