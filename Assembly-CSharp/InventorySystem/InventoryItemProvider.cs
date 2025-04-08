using System;
using System.Collections.Generic;
using System.Linq;
using GameCore;
using Hints;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp1344;
using InventorySystem.Searching;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using UnityEngine;
using UnityEngine.Pool;

namespace InventorySystem
{
	public static class InventoryItemProvider
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnRoleChanged += InventoryItemProvider.RoleChanged;
			StaticUnityMethods.OnUpdate += InventoryItemProvider.Update;
		}

		private static void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!InventoryItemProvider.InventoriesToReplenish.TryDequeue(out referenceHub))
			{
				return;
			}
			if (referenceHub == null)
			{
				return;
			}
			InventoryItemProvider.SpawnPreviousInventoryPickups(referenceHub);
		}

		public static void ServerGrantLoadout(ReferenceHub target, PlayerRoleBase role, bool resetInventory = true)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerGrantLoadout can only be executed on the server.");
			}
			Inventory inventory = target.inventory;
			List<ItemType> list = NorthwoodLib.Pools.ListPool<ItemType>.Shared.Rent();
			Dictionary<ItemType, ushort> dictionary = CollectionPool<Dictionary<ItemType, ushort>, KeyValuePair<ItemType, ushort>>.Get();
			InventoryItemProvider.TryAssignLoadout(role, ref list, ref dictionary);
			PlayerReceivingLoadoutEventArgs playerReceivingLoadoutEventArgs = new PlayerReceivingLoadoutEventArgs(target, list, dictionary, resetInventory);
			PlayerEvents.OnReceivingLoadout(playerReceivingLoadoutEventArgs);
			if (!playerReceivingLoadoutEventArgs.IsAllowed)
			{
				NorthwoodLib.Pools.ListPool<ItemType>.Shared.Return(list);
				CollectionPool<Dictionary<ItemType, ushort>, KeyValuePair<ItemType, ushort>>.Release(dictionary);
				return;
			}
			if (resetInventory)
			{
				while (inventory.UserInventory.Items.Count > 0)
				{
					inventory.ServerRemoveItem(inventory.UserInventory.Items.ElementAt(0).Key, null);
				}
				inventory.UserInventory.ReserveAmmo.Clear();
				inventory.SendAmmoNextFrame = true;
			}
			foreach (KeyValuePair<ItemType, ushort> keyValuePair in dictionary)
			{
				inventory.ServerAddAmmo(keyValuePair.Key, (int)keyValuePair.Value);
			}
			for (int i = 0; i < list.Count; i++)
			{
				ItemBase itemBase = inventory.ServerAddItem(list[i], ItemAddReason.StartingItem, 0, null);
				Action<ReferenceHub, ItemBase> onItemProvided = InventoryItemProvider.OnItemProvided;
				if (onItemProvided != null)
				{
					onItemProvided(target, itemBase);
				}
			}
			PlayerEvents.OnReceivedLoadout(new PlayerReceivedLoadoutEventArgs(target, list, dictionary, resetInventory));
			NorthwoodLib.Pools.ListPool<ItemType>.Shared.Return(list);
			CollectionPool<Dictionary<ItemType, ushort>, KeyValuePair<ItemType, ushort>>.Release(dictionary);
		}

		private static bool TryAssignLoadout(PlayerRoleBase role, ref List<ItemType> items, ref Dictionary<ItemType, ushort> ammo)
		{
			if (!role.ServerSpawnFlags.HasFlag(RoleSpawnFlags.AssignInventory))
			{
				return false;
			}
			Dictionary<ItemType, ushort> dictionary;
			ItemType[] array;
			if (!InventoryItemProvider.TryGetLoadout(role.RoleTypeId, out dictionary, out array))
			{
				return false;
			}
			foreach (ItemType itemType in array)
			{
				items.Add(itemType);
			}
			foreach (KeyValuePair<ItemType, ushort> keyValuePair in dictionary)
			{
				ammo.Add(keyValuePair.Key, keyValuePair.Value);
			}
			return true;
		}

		private static bool TryGetLoadout(RoleTypeId role, out Dictionary<ItemType, ushort> ammo, out ItemType[] items)
		{
			InventoryRoleInfo inventoryRoleInfo;
			if (!StartingInventories.DefinedInventories.TryGetValue(role, out inventoryRoleInfo))
			{
				ammo = null;
				items = null;
				return false;
			}
			ammo = inventoryRoleInfo.Ammo;
			items = inventoryRoleInfo.Items;
			return true;
		}

		private static void SpawnPreviousInventoryPickups(ReferenceHub hub)
		{
			List<ItemPickupBase> list;
			if (!InventoryItemProvider.PreviousInventoryPickups.TryGetValue(hub, out list))
			{
				return;
			}
			NetworkConnection connectionToClient = hub.connectionToClient;
			hub.transform.position = hub.transform.position;
			bool flag = HintDisplay.SuppressedReceivers.Add(connectionToClient);
			foreach (ItemPickupBase itemPickupBase in list)
			{
				if (!(itemPickupBase == null) && !itemPickupBase.Info.Locked)
				{
					SearchCompletor searchCompletor = SearchCompletor.FromPickup(hub.searchCoordinator, itemPickupBase, 3.4028234663852886E+38);
					if (searchCompletor.AllowPickupUponEscape && searchCompletor.ValidateStart())
					{
						searchCompletor.Complete();
					}
					else
					{
						itemPickupBase.transform.position = hub.transform.position;
					}
				}
			}
			InventoryItemProvider.PreviousInventoryPickups.Remove(hub);
			if (flag)
			{
				HintDisplay.SuppressedReceivers.Remove(connectionToClient);
			}
		}

		private static void RoleChanged(ReferenceHub ply, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Inventory inventory = ply.inventory;
			bool flag = InventoryItemProvider.KeepItemsAfterEscaping && newRole.ServerSpawnReason == RoleChangeReason.Escaped;
			if (flag)
			{
				List<ItemPickupBase> list = new List<ItemPickupBase>();
				BodyArmor bodyArmor;
				if (inventory.TryGetBodyArmor(out bodyArmor))
				{
					bodyArmor.DontRemoveExcessOnDrop = true;
				}
				HashSet<ushort> hashSet = NorthwoodLib.Pools.HashSetPool<ushort>.Shared.Rent();
				foreach (KeyValuePair<ushort, ItemBase> keyValuePair in inventory.UserInventory.Items)
				{
					ushort num;
					ItemBase itemBase;
					keyValuePair.Deconstruct(out num, out itemBase);
					ushort num2 = num;
					Scp1344Item scp1344Item = itemBase as Scp1344Item;
					if (scp1344Item != null)
					{
						scp1344Item.Status = Scp1344Status.Idle;
					}
					else
					{
						hashSet.Add(num2);
					}
				}
				foreach (ushort num3 in hashSet)
				{
					list.Add(inventory.ServerDropItem(num3));
				}
				NorthwoodLib.Pools.HashSetPool<ushort>.Shared.Return(hashSet);
				InventoryItemProvider.PreviousInventoryPickups[ply] = list;
			}
			InventoryItemProvider.ServerGrantLoadout(ply, newRole, !flag);
			InventoryItemProvider.InventoriesToReplenish.Enqueue(ply);
		}

		public static Action<ReferenceHub, ItemBase> OnItemProvided;

		private static readonly Dictionary<ReferenceHub, List<ItemPickupBase>> PreviousInventoryPickups = new Dictionary<ReferenceHub, List<ItemPickupBase>>();

		private static readonly Queue<ReferenceHub> InventoriesToReplenish = new Queue<ReferenceHub>();

		private static readonly bool KeepItemsAfterEscaping = ConfigFile.ServerConfig.GetBool("keep_items_after_escaping", true);
	}
}
