using System;
using System.Collections.Generic;
using InventorySystem;
using InventorySystem.Items;
using InventorySystem.Items.Keycards;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Christmas.Scp2536.Gifts
{
	public class KeycardUpgrade : Scp2536GiftBase
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Two;
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			if (base.CanBeGranted(hub))
			{
				return hub.inventory.UserInventory.Items.Any(delegate(KeyValuePair<ushort, ItemBase> i)
				{
					KeycardItem keycardItem = i.Value as KeycardItem;
					if (keycardItem == null)
					{
						return false;
					}
					ItemType itemTypeId = keycardItem.ItemTypeId;
					return itemTypeId - ItemType.KeycardMTFCaptain > 3;
				});
			}
			return false;
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			KeycardItem keycardItem = null;
			bool flag = false;
			foreach (ItemBase itemBase in hub.inventory.UserInventory.Items.Values)
			{
				KeycardItem keycardItem2 = itemBase as KeycardItem;
				if (keycardItem2 != null)
				{
					flag = true;
					keycardItem = keycardItem2;
					break;
				}
			}
			if (!flag)
			{
				Debug.LogError("Attempted to grant KeycardUpgrade to a player with no keycards.");
				return;
			}
			ItemType itemType = this.UpgradeKeycard(keycardItem.ItemTypeId);
			hub.inventory.ServerRemoveItem(keycardItem.ItemSerial, null);
			hub.inventory.ServerAddItem(itemType, ItemAddReason.Scp2536, 0, null).GrantAmmoReward();
		}

		private ItemType GenerateOutcome(params ItemType[] outcomes)
		{
			return outcomes[global::UnityEngine.Random.Range(0, outcomes.Length)];
		}

		public ItemType UpgradeKeycard(ItemType keyId)
		{
			switch (keyId)
			{
			case ItemType.KeycardJanitor:
				return this.GenerateOutcome(new ItemType[]
				{
					ItemType.KeycardZoneManager,
					ItemType.KeycardScientist
				});
			case ItemType.KeycardScientist:
				return ItemType.KeycardResearchCoordinator;
			case ItemType.KeycardResearchCoordinator:
				return ItemType.KeycardFacilityManager;
			case ItemType.KeycardZoneManager:
				return this.GenerateOutcome(new ItemType[]
				{
					ItemType.KeycardFacilityManager,
					ItemType.KeycardMTFOperative
				});
			case ItemType.KeycardGuard:
			case ItemType.KeycardMTFPrivate:
				return ItemType.KeycardMTFOperative;
			case ItemType.KeycardContainmentEngineer:
				return ItemType.KeycardFacilityManager;
			case ItemType.KeycardMTFOperative:
				return ItemType.KeycardMTFCaptain;
			default:
				return ItemType.KeycardChaosInsurgency;
			}
		}
	}
}
