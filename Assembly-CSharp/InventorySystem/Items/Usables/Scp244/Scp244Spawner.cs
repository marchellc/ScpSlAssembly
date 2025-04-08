using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using MapGeneration;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244
{
	public static class Scp244Spawner
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			NetIdWaypoint.OnNetIdWaypointsSet += Scp244Spawner.SpawnAllInstances;
		}

		private static void SpawnAllInstances()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Scp244Spawner.CompatibleRooms.Clear();
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(ItemType.SCP244b, out itemBase))
			{
				return;
			}
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				if (roomIdentifier != null && roomIdentifier.Zone == FacilityZone.HeavyContainment && Scp244Spawner.NameToPos.ContainsKey(roomIdentifier.Name))
				{
					Scp244Spawner.CompatibleRooms.Add(roomIdentifier);
				}
			}
			for (int i = 0; i < 1; i++)
			{
				Scp244Spawner.SpawnScp244(itemBase);
			}
		}

		private static void SpawnScp244(ItemBase ib)
		{
			if (Scp244Spawner.CompatibleRooms.Count == 0 || global::UnityEngine.Random.value > 0.35f)
			{
				return;
			}
			int num = global::UnityEngine.Random.Range(0, Scp244Spawner.CompatibleRooms.Count);
			Vector3 vector = Scp244Spawner.CompatibleRooms[num].transform.TransformPoint(Scp244Spawner.NameToPos[Scp244Spawner.CompatibleRooms[num].Name]);
			ItemPickupBase itemPickupBase = global::UnityEngine.Object.Instantiate<ItemPickupBase>(ib.PickupDropModel, vector, Quaternion.identity);
			itemPickupBase.NetworkInfo = new PickupSyncInfo
			{
				ItemId = ib.ItemTypeId,
				WeightKg = ib.Weight,
				Serial = ItemSerialGenerator.GenerateNext()
			};
			Scp244DeployablePickup scp244DeployablePickup = itemPickupBase as Scp244DeployablePickup;
			if (scp244DeployablePickup != null)
			{
				scp244DeployablePickup.State = Scp244State.Active;
			}
			NetworkServer.Spawn(itemPickupBase.gameObject, null);
			Scp244Spawner.CompatibleRooms.RemoveAt(num);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static Scp244Spawner()
		{
			Dictionary<RoomName, Vector3> dictionary = new Dictionary<RoomName, Vector3>();
			dictionary[RoomName.Unnamed] = Vector3.up;
			dictionary[RoomName.HczWarhead] = new Vector3(6.8f, 401f, 11.6f);
			dictionary[RoomName.HczMicroHID] = new Vector3(-7.4f, 1f, -6.8f);
			dictionary[RoomName.HczArmory] = new Vector3(-3.8f, 1f, 0.8f);
			dictionary[RoomName.HczTestroom] = new Vector3(0f, 0.26f, 7f);
			Scp244Spawner.NameToPos = dictionary;
		}

		private static readonly List<RoomIdentifier> CompatibleRooms = new List<RoomIdentifier>();

		private const int Amount = 1;

		private const float SpawnChance = 0.35f;

		private static readonly Dictionary<RoomName, Vector3> NameToPos;
	}
}
