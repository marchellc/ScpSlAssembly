using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration.RoomConnectors.Spawners
{
	public class AdvancedRoomConnectorSpawnpoint : RoomConnectorSpawnpointBase
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
			bool flag = false;
			foreach (AdvancedRoomConnectorSpawnpoint.ConnectorTypesChancePair connectorTypesChancePair in this.Connectors)
			{
				if (connectorTypesChancePair.Chance > 0f && connectorTypesChancePair.RoomConnectorTypes.Count != 0)
				{
					flag = true;
					if (connectorTypesChancePair.RoomConnectorTypes.Contains(type))
					{
						return connectorTypesChancePair.Chance;
					}
				}
			}
			if (flag)
			{
				return 0f;
			}
			return (float)((type == this._fallbackType) ? 0 : 1);
		}

		public override void SpawnFallback()
		{
			List<SpawnableRoomConnectorType> list = new List<SpawnableRoomConnectorType>();
			foreach (AdvancedRoomConnectorSpawnpoint.ConnectorTypesChancePair connectorTypesChancePair in this.Connectors)
			{
				list.AddRange(connectorTypesChancePair.RoomConnectorTypes);
			}
			if (list.Count > 0)
			{
				base.Spawn(list.RandomItem<SpawnableRoomConnectorType>());
				return;
			}
			base.SpawnFallback();
		}

		protected override void MergeWith(RoomConnectorSpawnpointBase retired)
		{
			foreach (AdvancedRoomConnectorSpawnpoint.ConnectorTypesChancePair connectorTypesChancePair in this.Connectors)
			{
				for (int j = connectorTypesChancePair.RoomConnectorTypes.Count - 1; j >= 0; j--)
				{
					if (retired.GetSpawnChanceWeight(connectorTypesChancePair.RoomConnectorTypes[j]) <= 0f)
					{
						connectorTypesChancePair.RoomConnectorTypes.RemoveAt(j);
					}
				}
			}
		}

		public AdvancedRoomConnectorSpawnpoint.ConnectorTypesChancePair[] Connectors;

		[SerializeField]
		private SpawnableRoomConnectorType _fallbackType;

		[Serializable]
		public struct ConnectorTypesChancePair
		{
			public float Chance;

			public List<SpawnableRoomConnectorType> RoomConnectorTypes;
		}
	}
}
