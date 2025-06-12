using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace MapGeneration.Distributors;

public abstract class SpawnablesDistributorBase : MonoBehaviour
{
	public static readonly HashSet<Rigidbody> BodiesToUnfreeze = new HashSet<Rigidbody>();

	private readonly Dictionary<DoorVariant, HashSet<GameObject>> _unspawnedObjects = new Dictionary<DoorVariant, HashSet<GameObject>>();

	[SerializeField]
	protected SpawnablesDistributorSettings Settings;

	private bool _eventsRegistered;

	private bool _placed;

	private bool _unfrozen;

	private readonly Stopwatch _stopwatch = new Stopwatch();

	private void Awake()
	{
		if (NetworkServer.active)
		{
			this._eventsRegistered = true;
			CustomNetworkManager.OnClientReady += OnClientReady;
			SeedSynchronizer.OnGenerationStage += OnMapStage;
			Scp079Camera.OnAnyCameraStateChanged += On079CamChanged;
			DoorEvents.OnDoorAction += OnDoorAction;
			PocketDimensionTeleport.OnPlayerEscapePocketDimension += OnPlayerEscapePocketDimension;
			this.Register();
		}
	}

	private void OnDestroy()
	{
		if (this._eventsRegistered)
		{
			CustomNetworkManager.OnClientReady -= OnClientReady;
			SeedSynchronizer.OnGenerationStage -= OnMapStage;
			Scp079Camera.OnAnyCameraStateChanged -= On079CamChanged;
			DoorEvents.OnDoorAction -= OnDoorAction;
			PocketDimensionTeleport.OnPlayerEscapePocketDimension -= OnPlayerEscapePocketDimension;
			this.Unregister();
		}
	}

	private void OnMapStage(MapGenerationPhase stage)
	{
		if (stage == MapGenerationPhase.SpawnableStructures && NetworkClient.ready)
		{
			this._stopwatch.Restart();
		}
	}

	private void OnClientReady()
	{
		if (SeedSynchronizer.MapGenerated)
		{
			this._stopwatch.Restart();
		}
	}

	private void Update()
	{
		if (!NetworkServer.active || !NetworkClient.active || !this._stopwatch.IsRunning)
		{
			return;
		}
		float num = (float)this._stopwatch.Elapsed.TotalSeconds;
		if (!this._placed && num > this.Settings.SpawnerDelay)
		{
			this._placed = true;
			this.PlaceSpawnables();
		}
		else if (!this._unfrozen && num > this.Settings.UnfreezeDelay)
		{
			foreach (Rigidbody item in SpawnablesDistributorBase.BodiesToUnfreeze)
			{
				if (item != null)
				{
					item.isKinematic = false;
				}
			}
			SpawnablesDistributorBase.BodiesToUnfreeze.Clear();
			this._unfrozen = true;
		}
		if (this._placed && this._unfrozen)
		{
			this._stopwatch.Stop();
		}
	}

	private void OnDoorAction(DoorVariant door, DoorAction action, ReferenceHub hub)
	{
		if (action == DoorAction.Opened || action == DoorAction.Destroyed)
		{
			this.SpawnForDoor(door);
		}
	}

	private void On079CamChanged(Scp079Camera cam)
	{
		if (!cam.IsActive || !DoorVariant.DoorsByRoom.TryGetValue(cam.Room, out var value))
		{
			return;
		}
		foreach (DoorVariant item in value)
		{
			this.SpawnForDoor(item);
		}
	}

	private void OnPlayerEscapePocketDimension(ReferenceHub hub)
	{
		if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole) || !fpcRole.FpcModule.Position.TryGetRoom(out var room) || !DoorVariant.DoorsByRoom.TryGetValue(room, out var value))
		{
			return;
		}
		foreach (DoorVariant item in value)
		{
			this.SpawnForDoor(item);
		}
	}

	protected abstract void PlaceSpawnables();

	protected virtual void Register()
	{
	}

	protected virtual void Unregister()
	{
	}

	protected void RegisterUnspawnedObject(DoorVariant door, GameObject unspawnedObject)
	{
		if (this._unspawnedObjects.TryGetValue(door, out var value) && value != null)
		{
			value.Add(unspawnedObject);
			return;
		}
		this._unspawnedObjects[door] = new HashSet<GameObject> { unspawnedObject };
	}

	protected virtual void SpawnObject(GameObject objectToSpawn)
	{
		if (objectToSpawn != null)
		{
			NetworkServer.Spawn(objectToSpawn);
		}
	}

	public void SpawnForDoor(DoorVariant door)
	{
		if (!this._unspawnedObjects.TryGetValue(door, out var value) || value == null)
		{
			return;
		}
		foreach (GameObject item in value)
		{
			this.SpawnObject(item);
		}
		this._unspawnedObjects.Remove(door);
	}
}
