using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class ItemDistributor : SpawnablesDistributorBase
	{
		protected override void PlaceSpawnables()
		{
			while (ItemSpawnpoint.RandomInstances.Remove(null))
			{
			}
			while (ItemSpawnpoint.AutospawnInstances.Remove(null))
			{
			}
			foreach (SpawnableItem spawnableItem in this.Settings.SpawnableItems)
			{
				this.PlaceItem(spawnableItem);
			}
			foreach (ItemSpawnpoint itemSpawnpoint in ItemSpawnpoint.AutospawnInstances)
			{
				Transform transform = itemSpawnpoint.Occupy();
				this.CreatePickup(itemSpawnpoint.AutospawnItem, transform, itemSpawnpoint.TriggerDoorName);
			}
		}

		private void PlaceItem(SpawnableItem item)
		{
			float num = global::UnityEngine.Random.Range(item.MinimalAmount, item.MaxAmount);
			List<ItemSpawnpoint> list = ListPool<ItemSpawnpoint>.Shared.Rent();
			foreach (ItemSpawnpoint itemSpawnpoint in ItemSpawnpoint.RandomInstances)
			{
				if (item.RoomNames.Contains(itemSpawnpoint.RoomName) && itemSpawnpoint.CanSpawn(item.PossibleSpawns))
				{
					list.Add(itemSpawnpoint);
				}
			}
			if (item.MultiplyBySpawnpointsNumber)
			{
				num *= (float)list.Count;
			}
			int num2 = 0;
			while ((float)num2 < num && list.Count != 0)
			{
				ItemType itemType = item.PossibleSpawns[global::UnityEngine.Random.Range(0, item.PossibleSpawns.Length)];
				if (itemType != ItemType.None)
				{
					int num3 = global::UnityEngine.Random.Range(0, list.Count);
					Transform transform = list[num3].Occupy();
					this.CreatePickup(itemType, transform, list[num3].TriggerDoorName);
					if (!list[num3].CanSpawn(itemType))
					{
						list.RemoveAt(num3);
					}
				}
				num2++;
			}
			ListPool<ItemSpawnpoint>.Shared.Return(list);
		}

		private void CreatePickup(ItemType id, Transform t, string triggerDoor)
		{
			ItemSpawningEventArgs itemSpawningEventArgs = new ItemSpawningEventArgs(id);
			ServerEvents.OnItemSpawning(itemSpawningEventArgs);
			if (!itemSpawningEventArgs.IsAllowed)
			{
				return;
			}
			ItemBase itemBase;
			if (!InventoryItemLoader.AvailableItems.TryGetValue(id, out itemBase))
			{
				return;
			}
			if (itemBase.PickupDropModel == null)
			{
				return;
			}
			ItemPickupBase itemPickupBase = global::UnityEngine.Object.Instantiate<ItemPickupBase>(itemBase.PickupDropModel, t.position, t.rotation);
			itemPickupBase.NetworkInfo = new PickupSyncInfo(id, itemBase.Weight, 0, false);
			itemPickupBase.transform.SetParent(t);
			IPickupDistributorTrigger pickupDistributorTrigger = itemPickupBase as IPickupDistributorTrigger;
			if (pickupDistributorTrigger != null)
			{
				pickupDistributorTrigger.OnDistributed();
			}
			DoorNametagExtension doorNametagExtension;
			if (string.IsNullOrEmpty(triggerDoor) || !DoorNametagExtension.NamedDoors.TryGetValue(triggerDoor, out doorNametagExtension))
			{
				ItemDistributor.SpawnPickup(itemPickupBase);
			}
			else
			{
				base.RegisterUnspawnedObject(doorNametagExtension.TargetDoor, itemPickupBase.gameObject);
			}
			ServerEvents.OnItemSpawned(new ItemSpawnedEventArgs(itemPickupBase));
		}

		public static void SpawnPickup(ItemPickupBase ipb)
		{
			if (ipb == null)
			{
				return;
			}
			NetworkServer.Spawn(ipb.gameObject, null);
			PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(ipb.Info.ItemId, ipb.Info.WeightKg, 0, false)
			{
				Locked = ipb.Info.Locked
			};
			InitiallySpawnedItems.Singleton.AddInitial(pickupSyncInfo.Serial);
			ipb.NetworkInfo = pickupSyncInfo;
		}

		protected override void SpawnObject(GameObject objectToSpawn)
		{
			ItemPickupBase itemPickupBase;
			if (objectToSpawn != null && objectToSpawn.TryGetComponent<ItemPickupBase>(out itemPickupBase))
			{
				ItemDistributor.SpawnPickup(itemPickupBase);
			}
		}
	}
}
