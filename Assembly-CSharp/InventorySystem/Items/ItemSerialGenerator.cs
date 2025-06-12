namespace InventorySystem.Items;

public static class ItemSerialGenerator
{
	private static ushort _ai;

	public static void Reset()
	{
		ItemSerialGenerator._ai = 0;
	}

	public static ushort GenerateNext()
	{
		if (ItemSerialGenerator._ai > 65000)
		{
			ItemSerialGenerator.Reset();
		}
		ItemSerialGenerator._ai++;
		return ItemSerialGenerator._ai;
	}
}
