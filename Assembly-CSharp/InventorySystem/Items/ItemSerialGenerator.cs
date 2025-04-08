using System;

namespace InventorySystem.Items
{
	public static class ItemSerialGenerator
	{
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
			ItemSerialGenerator._ai += 1;
			return ItemSerialGenerator._ai;
		}

		private static ushort _ai;
	}
}
