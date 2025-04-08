using System;
using System.Collections.Generic;
using Mirror;

namespace InventorySystem.Disarming
{
	public readonly struct DisarmedPlayersListMessage : NetworkMessage
	{
		public DisarmedPlayersListMessage(List<DisarmedPlayers.DisarmedEntry> entries)
		{
			this.Entries = entries;
		}

		public readonly List<DisarmedPlayers.DisarmedEntry> Entries;
	}
}
