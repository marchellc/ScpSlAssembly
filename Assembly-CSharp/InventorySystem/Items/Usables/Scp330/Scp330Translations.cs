namespace InventorySystem.Items.Usables.Scp330;

public static class Scp330Translations
{
	public enum Entry
	{
		Candies
	}

	private const int CandiesOffset = 1;

	public static string GetSpecificTranslation(int index, string fallback)
	{
		return TranslationReader.Get("SCP330", index, fallback);
	}

	public static string GetEntryTranslation(Entry entry)
	{
		return Scp330Translations.GetSpecificTranslation((int)entry, entry.ToString());
	}

	public static void GetCandyTranslation(CandyKindID candyKind, out string name, out string desc, out string fx)
	{
		int num = 1 + (int)candyKind * 3;
		name = Scp330Translations.GetSpecificTranslation(num - 3, candyKind.ToString());
		desc = Scp330Translations.GetSpecificTranslation(num - 2, "No Description");
		fx = Scp330Translations.GetSpecificTranslation(num - 1, "Unknown effects");
	}
}
