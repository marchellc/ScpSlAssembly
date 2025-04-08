using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public abstract class SpawnablesDistributorBase : MonoBehaviour
	{
		private void Awake()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._eventsRegistered = true;
			SeedSynchronizer.OnGenerationStage += this.OnMapStage;
			Scp079Camera.OnAnyCameraStateChanged += this.On079CamChanged;
			DoorEvents.OnDoorAction += this.OnDoorAction;
			PocketDimensionTeleport.OnPlayerEscapePocketDimension += this.OnPlayerEscapePocketDimension;
			this.Register();
		}

		private void OnDestroy()
		{
			if (this._eventsRegistered)
			{
				SeedSynchronizer.OnGenerationStage -= this.OnMapStage;
				Scp079Camera.OnAnyCameraStateChanged -= this.On079CamChanged;
				DoorEvents.OnDoorAction -= this.OnDoorAction;
				PocketDimensionTeleport.OnPlayerEscapePocketDimension -= this.OnPlayerEscapePocketDimension;
				this.Unregister();
			}
		}

		private void OnMapStage(MapGenerationPhase stage)
		{
			if (stage != MapGenerationPhase.SpawnableStructures)
			{
				return;
			}
			this._stopwatch.Restart();
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
				this.PlaceSpawnables();
				this._placed = true;
			}
			else if (!this._unfrozen && num > this.Settings.UnfreezeDelay)
			{
				foreach (Rigidbody rigidbody in SpawnablesDistributorBase.BodiesToUnfreeze)
				{
					if (rigidbody != null)
					{
						rigidbody.isKinematic = false;
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
			if (action != DoorAction.Opened && action != DoorAction.Destroyed)
			{
				return;
			}
			this.SpawnForDoor(door);
		}

		private void On079CamChanged(Scp079Camera cam)
		{
			if (!cam.IsActive)
			{
				return;
			}
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(cam.Room, out hashSet))
			{
				return;
			}
			foreach (DoorVariant doorVariant in hashSet)
			{
				this.SpawnForDoor(doorVariant);
			}
		}

		private void OnPlayerEscapePocketDimension(ReferenceHub hub)
		{
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(hub.transform.position, true);
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(roomIdentifier, out hashSet))
			{
				return;
			}
			foreach (DoorVariant doorVariant in hashSet)
			{
				this.SpawnForDoor(doorVariant);
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
			HashSet<GameObject> hashSet;
			if (this._unspawnedObjects.TryGetValue(door, out hashSet) && hashSet != null)
			{
				hashSet.Add(unspawnedObject);
				return;
			}
			this._unspawnedObjects[door] = new HashSet<GameObject> { unspawnedObject };
		}

		protected virtual void SpawnObject(GameObject objectToSpawn)
		{
			if (objectToSpawn != null)
			{
				NetworkServer.Spawn(objectToSpawn, null);
			}
		}

		public void SpawnForDoor(DoorVariant door)
		{
			HashSet<GameObject> hashSet;
			if (!this._unspawnedObjects.TryGetValue(door, out hashSet) || hashSet == null)
			{
				return;
			}
			foreach (GameObject gameObject in hashSet)
			{
				this.SpawnObject(gameObject);
			}
			this._unspawnedObjects.Remove(door);
		}

		public static readonly HashSet<Rigidbody> BodiesToUnfreeze = new HashSet<Rigidbody>();

		private readonly Dictionary<DoorVariant, HashSet<GameObject>> _unspawnedObjects = new Dictionary<DoorVariant, HashSet<GameObject>>();

		[SerializeField]
		protected SpawnablesDistributorSettings Settings;

		private bool _eventsRegistered;

		private bool _placed;

		private bool _unfrozen;

		private readonly Stopwatch _stopwatch = new Stopwatch();
	}
}
