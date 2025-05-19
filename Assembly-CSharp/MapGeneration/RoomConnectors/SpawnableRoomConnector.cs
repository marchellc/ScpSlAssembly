using Mirror;

namespace MapGeneration.RoomConnectors;

public class SpawnableRoomConnector : NetworkBehaviour
{
	public RoomConnectorSpawnData SpawnData;

	public override bool Weaved()
	{
		return true;
	}
}
