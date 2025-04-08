using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Handlers;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using Scp914.Processors;
using UnityEngine;

namespace Scp914
{
	public static class Scp914Upgrader
	{
		public static event Action<Scp914Result, Scp914KnobSetting> OnUpgraded;

		public static void Upgrade(Collider[] intake, Scp914Mode mode, Scp914KnobSetting setting)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Scp914Upgrader.Upgrade is a serverside-only script.");
			}
			HashSet<GameObject> hashSet = HashSetPool<GameObject>.Shared.Rent();
			bool flag = (mode & Scp914Mode.Dropped) == Scp914Mode.Dropped;
			bool flag2 = (mode & Scp914Mode.Inventory) == Scp914Mode.Inventory;
			bool flag3 = flag2 && (mode & Scp914Mode.Held) == Scp914Mode.Held;
			for (int i = 0; i < intake.Length; i++)
			{
				GameObject gameObject = intake[i].transform.root.gameObject;
				if (hashSet.Add(gameObject))
				{
					ReferenceHub referenceHub;
					ItemPickupBase itemPickupBase;
					if (ReferenceHub.TryGetHub(gameObject, out referenceHub))
					{
						Scp914Upgrader.ProcessPlayer(referenceHub, flag2, flag3, setting);
					}
					else if (gameObject.TryGetComponent<ItemPickupBase>(out itemPickupBase))
					{
						Scp914Upgrader.ProcessPickup(itemPickupBase, flag, setting);
					}
				}
			}
			HashSetPool<GameObject>.Shared.Return(hashSet);
		}

		private static void ProcessPlayer(ReferenceHub ply, bool upgradeInventory, bool heldOnly, Scp914KnobSetting setting)
		{
			if (Physics.Linecast(ply.transform.position, Scp914Controller.Singleton.IntakeChamber.position, Scp914Upgrader.SolidObjectMask))
			{
				return;
			}
			Vector3 vector = ply.transform.position + Scp914Controller.MoveVector;
			Scp914ProcessingPlayerEventArgs scp914ProcessingPlayerEventArgs = new Scp914ProcessingPlayerEventArgs(vector, setting, ply);
			Scp914Events.OnProcessingPlayer(scp914ProcessingPlayerEventArgs);
			if (!scp914ProcessingPlayerEventArgs.IsAllowed)
			{
				return;
			}
			setting = scp914ProcessingPlayerEventArgs.KnobSetting;
			vector = scp914ProcessingPlayerEventArgs.NewPosition;
			ply.TryOverridePosition(vector);
			if (!upgradeInventory)
			{
				return;
			}
			HashSet<ushort> hashSet = HashSetPool<ushort>.Shared.Rent();
			foreach (KeyValuePair<ushort, ItemBase> keyValuePair in ply.inventory.UserInventory.Items)
			{
				if (!heldOnly || keyValuePair.Key == ply.inventory.CurItem.SerialNumber)
				{
					hashSet.Add(keyValuePair.Key);
				}
			}
			foreach (ushort num in hashSet)
			{
				ItemBase itemBase;
				Scp914ItemProcessor scp914ItemProcessor;
				if (ply.inventory.UserInventory.Items.TryGetValue(num, out itemBase) && Scp914Upgrader.TryGetProcessor(itemBase.ItemTypeId, out scp914ItemProcessor))
				{
					ItemType itemTypeId = itemBase.ItemTypeId;
					Scp914ProcessingInventoryItemEventArgs scp914ProcessingInventoryItemEventArgs = new Scp914ProcessingInventoryItemEventArgs(itemBase, setting, ply);
					Scp914Events.OnProcessingInventoryItem(scp914ProcessingInventoryItemEventArgs);
					if (scp914ProcessingInventoryItemEventArgs.IsAllowed)
					{
						setting = scp914ProcessingInventoryItemEventArgs.KnobSetting;
						Action<ItemBase, Scp914KnobSetting> onInventoryItemUpgraded = Scp914Upgrader.OnInventoryItemUpgraded;
						if (onInventoryItemUpgraded != null)
						{
							onInventoryItemUpgraded(itemBase, setting);
						}
						Scp914Result scp914Result = scp914ItemProcessor.UpgradeInventoryItem(setting, itemBase);
						Action<Scp914Result, Scp914KnobSetting> onUpgraded = Scp914Upgrader.OnUpgraded;
						if (onUpgraded != null)
						{
							onUpgraded(scp914Result, setting);
						}
						ItemBase itemBase2;
						if (scp914Result.ResultingItems == null || !scp914Result.ResultingItems.TryGet(0, out itemBase2))
						{
							itemBase2 = null;
						}
						if (itemBase2 != null)
						{
							Scp914Events.OnProcessedInventoryItem(new Scp914ProcessedInventoryItemEventArgs(itemTypeId, itemBase2, setting, ply));
						}
					}
				}
			}
			HashSetPool<ushort>.Shared.Return(hashSet);
			BodyArmor bodyArmor;
			ply.inventory.RemoveEverythingExceedingLimits(ply.inventory.TryGetBodyArmor(out bodyArmor) ? bodyArmor : null, true, true);
			Scp914Events.OnProcessedPlayer(new Scp914ProcessedPlayerEventArgs(vector, setting, ply));
		}

		private static void ProcessPickup(ItemPickupBase pickup, bool upgradeDropped, Scp914KnobSetting setting)
		{
			Scp914ItemProcessor scp914ItemProcessor;
			if (!pickup.Info.Locked && upgradeDropped && Scp914Upgrader.TryGetProcessor(pickup.Info.ItemId, out scp914ItemProcessor))
			{
				Vector3 vector = pickup.transform.position + Scp914Controller.MoveVector;
				ItemType itemId = pickup.Info.ItemId;
				Scp914ProcessingPickupEventArgs scp914ProcessingPickupEventArgs = new Scp914ProcessingPickupEventArgs(vector, setting, pickup);
				Scp914Events.OnProcessingPickup(scp914ProcessingPickupEventArgs);
				if (!scp914ProcessingPickupEventArgs.IsAllowed)
				{
					return;
				}
				vector = scp914ProcessingPickupEventArgs.NewPosition;
				setting = scp914ProcessingPickupEventArgs.KnobSetting;
				Action<ItemPickupBase, Scp914KnobSetting> onPickupUpgraded = Scp914Upgrader.OnPickupUpgraded;
				if (onPickupUpgraded != null)
				{
					onPickupUpgraded(pickup, setting);
				}
				Scp914Result scp914Result = scp914ItemProcessor.UpgradePickup(setting, pickup);
				Action<Scp914Result, Scp914KnobSetting> onUpgraded = Scp914Upgrader.OnUpgraded;
				if (onUpgraded != null)
				{
					onUpgraded(scp914Result, setting);
				}
				ItemPickupBase itemPickupBase;
				if (scp914Result.ResultingPickups == null || scp914Result.ResultingPickups.TryGet(0, out itemPickupBase))
				{
					itemPickupBase = null;
				}
				Scp914Events.OnProcessedPickup(new Scp914ProcessedPickupEventArgs(itemId, vector, setting, itemPickupBase));
			}
		}

		private static bool TryGetProcessor(ItemType itemType, out Scp914ItemProcessor processor)
		{
			ItemBase itemBase;
			if (InventoryItemLoader.AvailableItems.TryGetValue(itemType, out itemBase) && itemBase.TryGetComponent<Scp914ItemProcessor>(out processor))
			{
				return true;
			}
			processor = null;
			return false;
		}

		public static int SolidObjectMask;

		public static Action<ItemPickupBase, Scp914KnobSetting> OnPickupUpgraded = delegate(ItemPickupBase targetPickup, Scp914KnobSetting usedMode)
		{
		};

		public static Action<ItemBase, Scp914KnobSetting> OnInventoryItemUpgraded = delegate(ItemBase targetItem, Scp914KnobSetting usedMode)
		{
		};
	}
}
