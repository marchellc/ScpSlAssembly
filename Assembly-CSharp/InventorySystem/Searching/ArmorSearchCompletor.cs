using System;
using Hints;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching
{
	public class ArmorSearchCompletor : SearchCompletor
	{
		public override bool AllowPickupUponEscape
		{
			get
			{
				return false;
			}
		}

		public ArmorSearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
			: base(hub, targetPickup, targetItem, maxDistanceSquared)
		{
			this._armorType = targetItem.ItemTypeId;
		}

		protected override bool ValidateAny()
		{
			if (!base.ValidateAny())
			{
				return false;
			}
			bool flag = 8 > this.Hub.inventory.UserInventory.Items.Count;
			bool flag2 = this.Hub.inventory.TryGetBodyArmorAndItsSerial(out this._currentArmor, out this._currentArmorSerial);
			if (!flag && !flag2)
			{
				this.Hub.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[]
				{
					new ByteHintParameter(8)
				}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
				return false;
			}
			return true;
		}

		public override bool ValidateStart()
		{
			if (!base.ValidateStart())
			{
				return false;
			}
			if (this.TargetItem.ItemTypeId == ItemType.None)
			{
				throw new InvalidOperationException("Item has an invalid ItemType.");
			}
			if (this.TargetItem.Category == ItemCategory.Ammo)
			{
				throw new InvalidOperationException("Item is not equippable (can be held in inventory).");
			}
			PlayerSearchingArmorEventArgs playerSearchingArmorEventArgs = new PlayerSearchingArmorEventArgs(this.Hub, this.TargetPickup);
			PlayerEvents.OnSearchingArmor(playerSearchingArmorEventArgs);
			return playerSearchingArmorEventArgs.IsAllowed;
		}

		public override void Complete()
		{
			PlayerPickingUpArmorEventArgs playerPickingUpArmorEventArgs = new PlayerPickingUpArmorEventArgs(this.Hub, this.TargetPickup);
			PlayerEvents.OnPickingUpArmor(playerPickingUpArmorEventArgs);
			if (!playerPickingUpArmorEventArgs.IsAllowed)
			{
				return;
			}
			if (this._currentArmor != null)
			{
				this._currentArmor.DontRemoveExcessOnDrop = true;
				this.Hub.inventory.ServerDropItem(this._currentArmorSerial);
			}
			BodyArmor bodyArmor = this.Hub.inventory.ServerAddItem(this.TargetPickup.Info.ItemId, ItemAddReason.PickedUp, this.TargetPickup.Info.Serial, this.TargetPickup) as BodyArmor;
			this.Hub.inventory.RemoveEverythingExceedingLimits(bodyArmor, true, true);
			this.TargetPickup.DestroySelf();
			PlayerEvents.OnPickedUpArmor(new PlayerPickedUpArmorEventArgs(this.Hub, bodyArmor));
		}

		private readonly ItemType _armorType;

		private BodyArmor _currentArmor;

		private ushort _currentArmorSerial;
	}
}
