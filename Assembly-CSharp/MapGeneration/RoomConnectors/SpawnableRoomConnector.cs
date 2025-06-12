using System;
using Mirror;

namespace MapGeneration.RoomConnectors;

public class SpawnableRoomConnector : NetworkBehaviour
{
	public static Action<SpawnableRoomConnector> OnAdded;

	public static Action<SpawnableRoomConnector> OnRemoved;

	public RoomConnectorSpawnData SpawnData;

	protected virtual void Start()
	{
		SpawnableRoomConnector.OnAdded?.Invoke(this);
	}

	protected virtual void OnDestroy()
	{
		SpawnableRoomConnector.OnRemoved?.Invoke(this);
	}

	public override bool Weaved()
	{
		return true;
	}
}
