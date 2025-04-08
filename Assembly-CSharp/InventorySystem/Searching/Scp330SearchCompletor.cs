using System;
using Hints;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching
{
	public class Scp330SearchCompletor : SearchCompletor
	{
		public Scp330SearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
			: base(hub, targetPickup, targetItem, maxDistanceSquared)
		{
			Scp330Bag.TryGetBag(hub, out this._playerBag);
		}

		protected override bool ValidateAny()
		{
			if (!base.ValidateAny())
			{
				return false;
			}
			bool flag = this._playerBag != null;
			int count = this.Hub.inventory.UserInventory.Items.Count;
			if ((!flag && count < 8) || (flag && this._playerBag.Candies.Count < 6))
			{
				return true;
			}
			Scp330SearchCompletor.ShowOverloadHint(this.Hub, flag);
			return false;
		}

		public static void ShowOverloadHint(ReferenceHub ply, bool hasBag)
		{
			if (hasBag)
			{
				ply.hints.Show(new TranslationHint(HintTranslations.MaxItemCategoryAlreadyReached, new HintParameter[]
				{
					new Scp330HintParameter(Scp330Translations.Entry.Candies),
					new ByteHintParameter(6)
				}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 2) }, 2f));
				return;
			}
			ply.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[]
			{
				new ByteHintParameter(8)
			}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
		}

		public override void Complete()
		{
			Scp330Pickup scp330Pickup = this.TargetPickup as Scp330Pickup;
			if (scp330Pickup == null)
			{
				return;
			}
			PlayerPickingUpScp330EventArgs playerPickingUpScp330EventArgs = new PlayerPickingUpScp330EventArgs(this.Hub, scp330Pickup);
			PlayerEvents.OnPickingUpScp330(playerPickingUpScp330EventArgs);
			if (!playerPickingUpScp330EventArgs.IsAllowed)
			{
				return;
			}
			Scp330Bag scp330Bag = null;
			Scp330Bag.ServerProcessPickup(this.Hub, scp330Pickup, out scp330Bag);
			if (scp330Pickup.StoredCandies.Count == 0)
			{
				scp330Pickup.DestroySelf();
			}
			else
			{
				PickupSyncInfo info = this.TargetPickup.Info;
				info.InUse = false;
				this.TargetPickup.NetworkInfo = info;
			}
			PlayerEvents.OnPickedUpScp330(new PlayerPickedUpScp330EventArgs(this.Hub, scp330Pickup, scp330Bag));
		}

		private readonly Scp330Bag _playerBag;
	}
}
