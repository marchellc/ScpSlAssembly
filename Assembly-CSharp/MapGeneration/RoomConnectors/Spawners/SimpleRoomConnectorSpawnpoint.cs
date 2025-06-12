using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration.RoomConnectors.Spawners;

public class SimpleRoomConnectorSpawnpoint : RoomConnectorSpawnpointBase
{
	public List<SpawnableRoomConnectorType> RoomConnectorTypes;

	[SerializeField]
	private SpawnableRoomConnectorType _fallbackType;

	public override SpawnableRoomConnectorType FallbackType => this._fallbackType;

	public override float GetSpawnChanceWeight(SpawnableRoomConnectorType type)
	{
		return this.RoomConnectorTypes.Contains(type) ? 1 : 0;
	}

	protected override void MergeWith(RoomConnectorSpawnpointBase retired)
	{
		for (int num = this.RoomConnectorTypes.Count - 1; num >= 0; num--)
		{
			if (!(retired.GetSpawnChanceWeight(this.RoomConnectorTypes[num]) > 0f))
			{
				this.RoomConnectorTypes.RemoveAt(num);
			}
		}
		if (this.RoomConnectorTypes.Count == 0)
		{
			this.RoomConnectorTypes.Add(this._fallbackType);
		}
	}
}
