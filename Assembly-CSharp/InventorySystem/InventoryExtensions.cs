using System;
using System.Collections.Generic;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using InventorySystem.Searching;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using NorthwoodLib.Pools;
using UnityEngine;

namespace InventorySystem
{
	public static class InventoryExtensions
	{
		public static event Action<ReferenceHub, ItemBase, ItemPickupBase> OnItemAdded;

		public static event Action<ReferenceHub, ItemBase, ItemPickupBase> OnItemRemoved;

		public static ItemType GetSelectedItemType(this Inventory inv)
		{
			if (inv.CurItem.SerialNumber <= 0)
			{
				return ItemType.None;
			}
			return inv.CurItem.TypeId;
		}

		public static bool TryGetHubHoldingSerial(ushort serial, out ReferenceHub hub)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.inventory.CurItem.SerialNumber == serial)
				{
					hub = referenceHub;
					return true;
				}
			}
			hub = null;
			return false;
		}

		public static bool ServerTryGetItemWithSerial(ushort serial, out ItemBase ib)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerTryGetItemWithSerial can only be executed on the server.");
			}
			using (HashSet<ReferenceHub>.Enumerator enumerator = ReferenceHub.AllHubs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.inventory.UserInventory.Items.TryGetValue(serial, out ib))
					{
						return true;
					}
				}
			}
			ib = null;
			return false;
		}

		public static ItemBase ServerAddItem(this Inventory inv, ItemType type, ItemAddReason addReason, ushort itemSerial = 0, ItemPickupBase pickup = null)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerAddItem can only be executed on the server.");
			}
			ItemBase itemBase;
			if (inv.UserInventory.Items.Count >= 8 && InventoryItemLoader.AvailableItems.TryGetValue(type, out itemBase) && itemBase.Category != ItemCategory.Ammo)
			{
				return null;
			}
			if (itemSerial == 0)
			{
				itemSerial = ItemSerialGenerator.GenerateNext();
			}
			ItemBase itemBase2 = inv.CreateItemInstance(new ItemIdentifier(type, itemSerial), inv.isLocalPlayer);
			if (itemBase2 == null)
			{
				return null;
			}
			inv.UserInventory.Items[itemSerial] = itemBase2;
			itemBase2.ServerAddReason = addReason;
			itemBase2.OnAdded(pickup);
			Action<ReferenceHub, ItemBase, ItemPickupBase> onItemAdded = InventoryExtensions.OnItemAdded;
			if (onItemAdded != null)
			{
				onItemAdded(inv._hub, itemBase2, pickup);
			}
			if (inv.isLocalPlayer)
			{
				IAcquisitionConfirmationTrigger acquisitionConfirmationTrigger = itemBase2 as IAcquisitionConfirmationTrigger;
				if (acquisitionConfirmationTrigger != null)
				{
					acquisitionConfirmationTrigger.ServerConfirmAcqusition();
					acquisitionConfirmationTrigger.AcquisitionAlreadyReceived = true;
				}
			}
			inv.SendItemsNextFrame = true;
			return itemBase2;
		}

		public static void ServerRemoveItem(this Inventory inv, ushort itemSerial, ItemPickupBase ipb)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerRemoveItem can only be executed on the server.");
			}
			ItemBase itemBase;
			if (!inv.DestroyItemInstance(itemSerial, ipb, out itemBase))
			{
				return;
			}
			if (itemSerial == inv.CurItem.SerialNumber)
			{
				inv.NetworkCurItem = ItemIdentifier.None;
			}
			inv.UserInventory.Items.Remove(itemSerial);
			inv.SendItemsNextFrame = true;
			Action<ReferenceHub, ItemBase, ItemPickupBase> onItemRemoved = InventoryExtensions.OnItemRemoved;
			if (onItemRemoved == null)
			{
				return;
			}
			onItemRemoved(inv._hub, itemBase, ipb);
		}

		public static ItemPickupBase ServerDropItem(this Inventory inv, ushort itemSerial)
		{
			ItemBase itemBase;
			if (!inv.UserInventory.Items.TryGetValue(itemSerial, out itemBase))
			{
				return null;
			}
			return itemBase.ServerDropItem(true);
		}

		public static void ServerDropEverything(this Inventory inv)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerDropEverything can only be executed on the server.");
			}
			HashSet<ItemType> hashSet = HashSetPool<ItemType>.Shared.Rent();
			foreach (KeyValuePair<ItemType, ushort> keyValuePair in inv.UserInventory.ReserveAmmo)
			{
				if (keyValuePair.Value > 0)
				{
					hashSet.Add(keyValuePair.Key);
				}
			}
			foreach (ItemType itemType in hashSet)
			{
				inv.ServerDropAmmo(itemType, ushort.MaxValue, false);
			}
			HashSetPool<ItemType>.Shared.Return(hashSet);
			HashSet<ushort> hashSet2 = HashSetPool<ushort>.Shared.Rent();
			foreach (ushort num in inv.UserInventory.Items.Keys)
			{
				hashSet2.Add(num);
			}
			foreach (ushort num2 in hashSet2)
			{
				inv.ServerDropItem(num2);
			}
			HashSetPool<ushort>.Shared.Return(hashSet2);
		}

		public static List<AmmoPickup> ServerDropAmmo(this Inventory inv, ItemType ammoType, ushort amount, bool checkMinimals = false)
		{
			List<AmmoPickup> list = new List<AmmoPickup>();
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerDropAmmo can only be executed on the server.");
			}
			ushort num;
			ItemBase itemBase;
			if (!inv.UserInventory.ReserveAmmo.TryGetValue(ammoType, out num) || !InventoryItemLoader.AvailableItems.TryGetValue(ammoType, out itemBase))
			{
				return list;
			}
			if (itemBase.PickupDropModel == null)
			{
				Debug.LogError("No pickup drop model set. Could not drop the ammo.");
				return list;
			}
			if (checkMinimals)
			{
				AmmoPickup ammoPickup = itemBase.PickupDropModel as AmmoPickup;
				if (ammoPickup != null)
				{
					int num2 = Mathf.FloorToInt((float)ammoPickup.SavedAmmo / 2f);
					if ((int)amount < num2 && (int)num > num2)
					{
						amount = (ushort)num2;
					}
				}
			}
			int i = Mathf.Min((int)amount, (int)num);
			PlayerDroppingAmmoEventArgs playerDroppingAmmoEventArgs = new PlayerDroppingAmmoEventArgs(inv._hub, ammoType, i);
			PlayerEvents.OnDroppingAmmo(playerDroppingAmmoEventArgs);
			if (!playerDroppingAmmoEventArgs.IsAllowed)
			{
				return list;
			}
			inv.UserInventory.ReserveAmmo[ammoType] = (ushort)((int)num - i);
			inv.SendAmmoNextFrame = true;
			while (i > 0)
			{
				PickupSyncInfo pickupSyncInfo = new PickupSyncInfo(ammoType, itemBase.Weight, 0, false);
				AmmoPickup ammoPickup2 = inv.ServerCreatePickup(itemBase, new PickupSyncInfo?(pickupSyncInfo), true, null) as AmmoPickup;
				if (ammoPickup2 != null)
				{
					list.Add(ammoPickup2);
					ushort num3 = (ushort)Mathf.Min(ammoPickup2.MaxAmmo, i);
					PlayerEvents.OnDroppedAmmo(new PlayerDroppedAmmoEventArgs(inv._hub, ammoType, (int)num3, ammoPickup2));
					ammoPickup2.NetworkSavedAmmo = num3;
					i -= (int)ammoPickup2.SavedAmmo;
				}
				else
				{
					i--;
				}
			}
			return list;
		}

		public static ItemPickupBase ServerCreatePickup(this Inventory inv, ItemBase item, PickupSyncInfo? psi, bool spawn = true, Action<ItemPickupBase> setupMethod = null)
		{
			Quaternion rotation = ReferenceHub.GetHub(inv.gameObject).PlayerCameraReference.rotation;
			Quaternion rotation2 = item.PickupDropModel.transform.rotation;
			return InventoryExtensions.ServerCreatePickup(item, psi, inv.transform.position, rotation * rotation2, spawn, setupMethod);
		}

		public static ItemPickupBase ServerCreatePickup(ItemBase item, PickupSyncInfo? psi, Vector3 position, bool spawn = true, Action<ItemPickupBase> setupMethod = null)
		{
			return InventoryExtensions.ServerCreatePickup(item, psi, position, item.PickupDropModel.transform.rotation, spawn, setupMethod);
		}

		public static ItemPickupBase ServerCreatePickup(ItemBase item, PickupSyncInfo? psi, Vector3 position, Quaternion rotation, bool spawn = true, Action<ItemPickupBase> setupMethod = null)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerCreatePickup can only be executed on the server.");
			}
			PickupSyncInfo pickupSyncInfo = psi.GetValueOrDefault();
			if (psi == null)
			{
				pickupSyncInfo = new PickupSyncInfo
				{
					ItemId = item.ItemTypeId,
					Serial = ItemSerialGenerator.GenerateNext(),
					WeightKg = item.Weight
				};
				psi = new PickupSyncInfo?(pickupSyncInfo);
			}
			ItemPickupBase itemPickupBase = global::UnityEngine.Object.Instantiate<ItemPickupBase>(item.PickupDropModel, position, rotation);
			itemPickupBase.NetworkInfo = psi.Value;
			if (setupMethod != null)
			{
				setupMethod(itemPickupBase);
			}
			if (spawn)
			{
				NetworkServer.Spawn(itemPickupBase.gameObject, null);
			}
			return itemPickupBase;
		}

		public static bool ServerAddOrDrop(this Inventory inv, ItemBase item, PickupSyncInfo psi, out ItemPickupBase ipb)
		{
			ipb = inv.ServerCreatePickup(item, new PickupSyncInfo?(psi), false, null);
			SearchCompletor searchCompletor = SearchCompletor.FromPickup(inv._hub.searchCoordinator, ipb, 3.4028234663852886E+38);
			if (searchCompletor.ValidateStart())
			{
				searchCompletor.Complete();
				return true;
			}
			NetworkServer.Spawn(ipb.gameObject, null);
			return false;
		}

		public static ushort GetCurAmmo(this Inventory inv, ItemType ammoType)
		{
			ushort num;
			if (!inv.UserInventory.ReserveAmmo.TryGetValue(ammoType, out num))
			{
				return 0;
			}
			return num;
		}

		public static void ServerSetAmmo(this Inventory inv, ItemType ammoType, int amount)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerSetAmmo can only be executed on the server.");
			}
			amount = Mathf.Clamp(amount, 0, 65535);
			inv.UserInventory.ReserveAmmo[ammoType] = (ushort)amount;
			inv.SendAmmoNextFrame = true;
		}

		public static void ServerAddAmmo(this Inventory inv, ItemType ammoType, int amountToAdd)
		{
			if (ammoType == ItemType.None)
			{
				return;
			}
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Method ServerAddAmmo can only be executed on the server.");
			}
			inv.ServerSetAmmo(ammoType, (int)inv.GetCurAmmo(ammoType) + amountToAdd);
		}

		public static bool TryGetInventoryItem(this Inventory inv, ItemType it, out ItemBase ib)
		{
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in inv.UserInventory.Items)
			{
				if (keyValuePair.Value.ItemTypeId == it)
				{
					ib = keyValuePair.Value;
					return true;
				}
			}
			ib = null;
			return false;
		}

		public static bool TryGetTemplate<T>(this ItemType itemType, out T item) where T : ItemBase
		{
			return InventoryItemLoader.TryGetItem<T>(itemType, out item);
		}

		public static T GetTemplate<T>(this ItemType itemType) where T : ItemBase
		{
			T t;
			if (!InventoryItemLoader.TryGetItem<T>(itemType, out t))
			{
				return default(T);
			}
			return t;
		}

		public static ItemBase GetTemplate(this ItemType itemType)
		{
			return InventoryItemLoader.AvailableItems.GetValueOrDefault(itemType);
		}

		public static Scp330Bag GrantCandy(this ReferenceHub hub, CandyKindID candyId, ItemAddReason itemAddReason)
		{
			bool flag = false;
			Scp330Bag scp330Bag;
			if (!Scp330Bag.TryGetBag(hub, out scp330Bag))
			{
				scp330Bag = hub.inventory.ServerAddItem(ItemType.SCP330, itemAddReason, 0, null) as Scp330Bag;
				flag = true;
			}
			if (scp330Bag == null)
			{
				return null;
			}
			if (flag)
			{
				scp330Bag.Candies = new List<CandyKindID> { candyId };
				scp330Bag.ServerRefreshBag();
			}
			else if (scp330Bag.TryAddSpecific(candyId))
			{
				scp330Bag.ServerRefreshBag();
			}
			return scp330Bag;
		}
	}
}
