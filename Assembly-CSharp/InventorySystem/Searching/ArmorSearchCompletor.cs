using System;
using Hints;
using InventorySystem.Items;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching;

public class ArmorSearchCompletor : PickupSearchCompletor
{
	private ItemCategory _category;

	private BodyArmor _currentArmor;

	public override bool AllowPickupUponEscape => false;

	public ArmorSearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
		: base(hub, targetPickup, maxDistanceSquared)
	{
		this._category = targetItem.Category;
	}

	protected override bool ValidateAny()
	{
		if (!base.ValidateAny())
		{
			return false;
		}
		bool num = 8 > base.Hub.inventory.UserInventory.Items.Count;
		bool flag = base.Hub.inventory.TryGetBodyArmor(out this._currentArmor);
		if (!num && !flag)
		{
			base.Hub.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[1]
			{
				new ByteHintParameter(8)
			}, new HintEffect[1] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
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
		if (base.TargetItemType == ItemType.None)
		{
			throw new InvalidOperationException("Item has an invalid ItemType.");
		}
		if (this._category == ItemCategory.Ammo)
		{
			throw new InvalidOperationException("Item is not equippable (can be held in inventory).");
		}
		PlayerSearchingArmorEventArgs e = new PlayerSearchingArmorEventArgs(base.Hub, base.TargetPickup as BodyArmorPickup);
		PlayerEvents.OnSearchingArmor(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		return true;
	}

	public override void Complete()
	{
		PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(base.Hub, base.TargetPickup));
		PlayerPickingUpArmorEventArgs e = new PlayerPickingUpArmorEventArgs(base.Hub, base.TargetPickup as BodyArmorPickup);
		PlayerEvents.OnPickingUpArmor(e);
		if (e.IsAllowed)
		{
			if (this._currentArmor != null)
			{
				base.Hub.inventory.ServerDropItem(this._currentArmor.ItemSerial);
			}
			BodyArmor armor = base.Hub.inventory.ServerAddItem(base.TargetPickup.Info.ItemId, ItemAddReason.PickedUp, base.TargetPickup.Info.Serial, base.TargetPickup) as BodyArmor;
			BodyArmorUtils.SetPlayerDirty(base.Hub);
			base.TargetPickup.DestroySelf();
			PlayerEvents.OnPickedUpArmor(new PlayerPickedUpArmorEventArgs(base.Hub, armor));
		}
	}
}
