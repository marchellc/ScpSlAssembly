using Hints;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;

namespace InventorySystem.Searching;

public class Scp330SearchCompletor : PickupSearchCompletor
{
	private readonly Scp330Bag _playerBag;

	public Scp330SearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, double maxDistanceSquared)
		: base(hub, targetPickup, maxDistanceSquared)
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
		int count = base.Hub.inventory.UserInventory.Items.Count;
		if ((!flag && count < 8) || (flag && this._playerBag.Candies.Count < 6))
		{
			return true;
		}
		Scp330SearchCompletor.ShowOverloadHint(base.Hub, flag);
		return false;
	}

	public static void ShowOverloadHint(ReferenceHub ply, bool hasBag)
	{
		if (hasBag)
		{
			ply.hints.Show(new TranslationHint(HintTranslations.MaxItemCategoryAlreadyReached, new HintParameter[2]
			{
				new Scp330HintParameter(Scp330Translations.Entry.Candies),
				new ByteHintParameter(6)
			}, new HintEffect[1] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 2) }, 2f));
		}
		else
		{
			ply.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[1]
			{
				new ByteHintParameter(8)
			}, new HintEffect[1] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
		}
	}

	public override void Complete()
	{
		if (!(base.TargetPickup is Scp330Pickup scp330Pickup))
		{
			return;
		}
		PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(base.Hub, base.TargetPickup));
		PlayerPickingUpScp330EventArgs e = new PlayerPickingUpScp330EventArgs(base.Hub, scp330Pickup);
		PlayerEvents.OnPickingUpScp330(e);
		if (e.IsAllowed)
		{
			Scp330Bag bag = null;
			Scp330Bag.ServerProcessPickup(base.Hub, scp330Pickup, out bag);
			if (scp330Pickup.StoredCandies.Count == 0)
			{
				scp330Pickup.DestroySelf();
			}
			else
			{
				PickupSyncInfo info = base.TargetPickup.Info;
				info.InUse = false;
				base.TargetPickup.NetworkInfo = info;
			}
			PlayerEvents.OnPickedUpScp330(new PlayerPickedUpScp330EventArgs(base.Hub, scp330Pickup, bag));
		}
	}
}
