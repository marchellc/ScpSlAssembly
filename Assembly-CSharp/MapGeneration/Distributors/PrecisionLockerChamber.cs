using System;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class PrecisionLockerChamber : LockerChamber
	{
		protected override void GetSpawnpoint(ItemType itemType, int index, out Vector3 worldPosition, out Quaternion worldRotation, out Transform parent)
		{
			foreach (PrecisionLockerChamber.SpawnpointOverride spawnpointOverride in this._spawnpointOverrides)
			{
				if (spawnpointOverride.AcceptedItemTypes.Contains(itemType))
				{
					parent = spawnpointOverride.Spawnpoint;
					spawnpointOverride.Spawnpoint.GetPositionAndRotation(out worldPosition, out worldRotation);
					return;
				}
			}
			base.GetSpawnpoint(itemType, index, out worldPosition, out worldRotation, out parent);
		}

		[SerializeField]
		private PrecisionLockerChamber.SpawnpointOverride[] _spawnpointOverrides;

		[Serializable]
		private struct SpawnpointOverride
		{
			public ItemType[] AcceptedItemTypes;

			public Transform Spawnpoint;
		}
	}
}
