using System;
using MapGeneration;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects;

public class ElevatorManager : MonoBehaviour
{
	[Serializable]
	private struct ChamberTypePair
	{
		public ElevatorGroup Group;

		public ElevatorChamber Prefab;
	}

	[SerializeField]
	private ChamberTypePair[] _customChambers;

	[SerializeField]
	private ElevatorChamber _defaultChamber;

	private void Awake()
	{
		this.RegisterElevatorPrefab(this._defaultChamber);
		this._customChambers.ForEach(delegate(ChamberTypePair x)
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
		SeedSynchronizer.OnGenerationFinished += SpawnAllChambers;
		if (SeedSynchronizer.MapGenerated)
		{
			this.SpawnAllChambers();
		}
	}

	private void OnDestroy()
	{
		SeedSynchronizer.OnGenerationFinished -= SpawnAllChambers;
	}

	private void SpawnAllChambers()
	{
		if (NetworkServer.active)
		{
			EnumUtils<ElevatorGroup>.Values.ForEach(SpawnChamber);
		}
	}

	private void SpawnChamber(ElevatorGroup group)
	{
		if (ElevatorDoor.GetDoorsForGroup(group).Count == 0)
		{
			return;
		}
		ElevatorChamber original = this._defaultChamber;
		ChamberTypePair[] customChambers = this._customChambers;
		for (int i = 0; i < customChambers.Length; i++)
		{
			ChamberTypePair chamberTypePair = customChambers[i];
			if (chamberTypePair.Group == group)
			{
				original = chamberTypePair.Prefab;
				break;
			}
		}
		ElevatorChamber elevatorChamber = UnityEngine.Object.Instantiate(original, null);
		elevatorChamber.NetworkAssignedGroup = group;
		NetworkServer.Spawn(elevatorChamber.gameObject);
	}
}
