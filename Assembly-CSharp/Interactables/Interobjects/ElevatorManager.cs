using System;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class ElevatorManager : MonoBehaviour
	{
		private void Awake()
		{
			this.RegisterElevatorPrefab(this._defaultChamber);
			this._customChambers.ForEach(delegate(ElevatorManager.ChamberTypePair x)
			{
				this.RegisterElevatorPrefab(x.Prefab);
			});
		}

		private void RegisterElevatorPrefab(ElevatorChamber chamber)
		{
			NetworkClient.prefabs[chamber.netIdentity.assetId] = chamber.gameObject;
		}

		private void Start()
		{
			SeedSynchronizer.OnGenerationFinished += this.SpawnAllChambers;
			if (SeedSynchronizer.MapGenerated)
			{
				this.SpawnAllChambers();
			}
		}

		private void OnDestroy()
		{
			SeedSynchronizer.OnGenerationFinished -= this.SpawnAllChambers;
		}

		private void SpawnAllChambers()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			EnumUtils<ElevatorGroup>.Values.ForEach(new Action<ElevatorGroup>(this.SpawnChamber));
		}

		private void SpawnChamber(ElevatorGroup group)
		{
			if (ElevatorDoor.GetDoorsForGroup(group).Count == 0)
			{
				return;
			}
			ElevatorChamber elevatorChamber = this._defaultChamber;
			foreach (ElevatorManager.ChamberTypePair chamberTypePair in this._customChambers)
			{
				if (chamberTypePair.Group == group)
				{
					elevatorChamber = chamberTypePair.Prefab;
					break;
				}
			}
			ElevatorChamber elevatorChamber2 = global::UnityEngine.Object.Instantiate<ElevatorChamber>(elevatorChamber, null);
			elevatorChamber2.NetworkAssignedGroup = group;
			NetworkServer.Spawn(elevatorChamber2.gameObject, null);
		}

		[SerializeField]
		private ElevatorManager.ChamberTypePair[] _customChambers;

		[SerializeField]
		private ElevatorChamber _defaultChamber;

		[Serializable]
		private struct ChamberTypePair
		{
			public ElevatorGroup Group;

			public ElevatorChamber Prefab;
		}
	}
}
