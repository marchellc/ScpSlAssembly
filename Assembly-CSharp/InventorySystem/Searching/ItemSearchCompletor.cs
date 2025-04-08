using System;
using System.Collections.Generic;
using Hints;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using UnityEngine;

namespace InventorySystem.Searching
{
	public class ItemSearchCompletor : SearchCompletor
	{
		private sbyte CategoryCount
		{
			get
			{
				sbyte b = 0;
				using (Dictionary<ushort, ItemBase>.ValueCollection.Enumerator enumerator = this.Hub.inventory.UserInventory.Items.Values.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Category == this._category)
						{
							b += 1;
						}
					}
				}
				return b;
			}
		}

		public ItemSearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
			: base(hub, targetPickup, targetItem, maxDistanceSquared)
		{
			this._category = targetItem.Category;
		}

		protected override bool ValidateAny()
		{
			if (!base.ValidateAny())
			{
				return false;
			}
			if (this.Hub.inventory.UserInventory.Items.Count >= 8)
			{
				this.Hub.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[]
				{
					new ByteHintParameter(8)
				}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
				return false;
			}
			if (this._category != ItemCategory.None)
			{
				int num = Mathf.Abs((int)InventoryLimits.GetCategoryLimit(this._category, this.Hub));
				if ((int)this.CategoryCount >= num)
				{
					this.Hub.hints.Show(new TranslationHint(HintTranslations.MaxItemCategoryAlreadyReached, new HintParameter[]
					{
						new ItemCategoryHintParameter(this._category),
						new ByteHintParameter((byte)num)
					}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 2) }, 2f));
					return false;
				}
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
			return true;
		}

		public override void Complete()
		{
			PlayerPickingUpItemEventArgs playerPickingUpItemEventArgs = new PlayerPickingUpItemEventArgs(this.Hub, this.TargetPickup);
			PlayerEvents.OnPickingUpItem(playerPickingUpItemEventArgs);
			if (!playerPickingUpItemEventArgs.IsAllowed)
			{
				return;
			}
			ItemBase itemBase = this.Hub.inventory.ServerAddItem(this.TargetPickup.Info.ItemId, ItemAddReason.PickedUp, this.TargetPickup.Info.Serial, this.TargetPickup);
			this.TargetPickup.DestroySelf();
			this.CheckCategoryLimitHint();
			PlayerEvents.OnPickedUpItem(new PlayerPickedUpItemEventArgs(this.Hub, itemBase));
		}

		protected void CheckCategoryLimitHint()
		{
			sbyte categoryLimit = InventoryLimits.GetCategoryLimit(this._category, this.Hub);
			if (this._category == ItemCategory.None || categoryLimit < 0 || this.CategoryCount < categoryLimit)
			{
				return;
			}
			HintEffect[] array = HintEffectPresets.FadeInAndOut(0.25f, 1f, 0f);
			HintParameter[] array2 = new HintParameter[]
			{
				new ItemCategoryHintParameter(this._category),
				new ByteHintParameter((byte)categoryLimit)
			};
			this.Hub.hints.Show(new TranslationHint(HintTranslations.MaxItemCategoryReached, array2, array, 1.5f));
		}

		private readonly ItemCategory _category;
	}
}
