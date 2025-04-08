using System;
using System.Collections.Generic;
using Mirror;

namespace InventorySystem.Disarming
{
	public static class DisarmedPlayersListMessageSerializers
	{
		public static void Serialize(this NetworkWriter writer, DisarmedPlayersListMessage value)
		{
			writer.WriteByte((byte)value.Entries.Count);
			foreach (DisarmedPlayers.DisarmedEntry disarmedEntry in value.Entries)
			{
				writer.WriteUInt(disarmedEntry.DisarmedPlayer);
				writer.WriteUInt(disarmedEntry.Disarmer);
			}
		}

		public static DisarmedPlayersListMessage Deserialize(this NetworkReader reader)
		{
			List<DisarmedPlayers.DisarmedEntry> list = new List<DisarmedPlayers.DisarmedEntry>();
			int num = (int)reader.ReadByte();
			for (int i = 0; i < num; i++)
			{
				uint num2 = reader.ReadUInt();
				uint num3 = reader.ReadUInt();
				list.Add(new DisarmedPlayers.DisarmedEntry(num2, num3));
			}
			return new DisarmedPlayersListMessage(list);
		}
	}
}
