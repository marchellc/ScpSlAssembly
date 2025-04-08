using System;
using Mirror;

namespace MapGeneration.RoomConnectors
{
	public class SpawnableRoomConnector : NetworkBehaviour
	{
		public override bool Weaved()
		{
			return true;
		}

		public RoomConnectorSpawnData SpawnData;
	}
}
