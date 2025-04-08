using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration.RoomConnectors.Spawners
{
	public class SimpleRoomConnectorSpawnpoint : RoomConnectorSpawnpointBase
	{
		public override SpawnableRoomConnectorType FallbackType
		{
			get
			{
				return this._fallbackType;
			}
		}

		public override float GetSpawnChanceWeight(SpawnableRoomConnectorType type)
		{
			return (float)(this.RoomConnectorTypes.Contains(type) ? 1 : 0);
		}

		protected override void MergeWith(RoomConnectorSpawnpointBase retired)
		{
			for (int i = this.RoomConnectorTypes.Count - 1; i >= 0; i--)
			{
				if (retired.GetSpawnChanceWeight(this.RoomConnectorTypes[i]) <= 0f)
				{
					this.RoomConnectorTypes.RemoveAt(i);
				}
			}
			if (this.RoomConnectorTypes.Count != 0)
			{
				return;
			}
			this.RoomConnectorTypes.Add(this._fallbackType);
		}

		public List<SpawnableRoomConnectorType> RoomConnectorTypes;

		[SerializeField]
		private SpawnableRoomConnectorType _fallbackType;
	}
}
