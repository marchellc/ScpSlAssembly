using System;
using Hints;
using InventorySystem.Configs;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using UnityEngine;

namespace InventorySystem.Searching;

public class AmmoSearchCompletor : PickupSearchCompletor
{
	private readonly ItemType _ammoType;

	private ushort CurrentAmmo
	{
		get
		{
			return base.Hub.inventory.GetCurAmmo(this._ammoType);
		}
		set
		{
			base.Hub.inventory.ServerSetAmmo(this._ammoType, value);
		}
	}

	private ushort MaxAmmo => InventoryLimits.GetAmmoLimit(this._ammoType, base.Hub);

	public AmmoSearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, double maxDistanceSquared)
		: base(hub, targetPickup, maxDistanceSquared)
	{
		this._ammoType = targetPickup.Info.ItemId;
	}

	public override bool ValidateStart()
	{
		if (!base.ValidateStart())
		{
			return false;
		}
		PlayerSearchingAmmoEventArgs e = new PlayerSearchingAmmoEventArgs(base.Hub, base.TargetPickup as AmmoPickup);
		PlayerEvents.OnSearchingAmmo(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		return true;
	}

	protected override bool ValidateAny()
	{
		if (!base.ValidateAny())
		{
			return false;
		}
		uint maxAmmo = this.MaxAmmo;
		if (this.CurrentAmmo >= maxAmmo)
		{
			base.Hub.hints.Show(new TranslationHint(HintTranslations.MaxAmmoAlreadyReached, new HintParameter[2]
			{
				new AmmoHintParameter((byte)this._ammoType),
				new PackedULongHintParameter(maxAmmo)
			}, new HintEffect[1] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 2) }, 2f));
			return false;
		}
		return true;
	}

	public override void Complete()
	{
		PlayerEvents.OnSearchedPickup(new PlayerSearchedPickupEventArgs(base.Hub, base.TargetPickup));
		if (!(base.TargetPickup is AmmoPickup ammoPickup))
		{
			Debug.LogError("The pickup needs to derive from AmmoPickup");
			return;
		}
		ushort currentAmmo = this.CurrentAmmo;
		ushort ammoAmount = (ushort)(Math.Min(currentAmmo + ammoPickup.SavedAmmo, this.MaxAmmo) - currentAmmo);
		PlayerPickingUpAmmoEventArgs e = new PlayerPickingUpAmmoEventArgs(base.Hub, this._ammoType, ammoAmount, base.TargetPickup as AmmoPickup);
		PlayerEvents.OnPickingUpAmmo(e);
		if (e.IsAllowed)
		{
			ammoAmount = e.AmmoAmount;
			if (ammoAmount >= ammoPickup.SavedAmmo)
			{
				base.TargetPickup.DestroySelf();
			}
			else
			{
				ammoPickup.NetworkSavedAmmo = (ushort)(ammoPickup.SavedAmmo - ammoAmount);
				PickupSyncInfo info = base.TargetPickup.Info;
				info.InUse = false;
				base.TargetPickup.NetworkInfo = info;
				base.Hub.hints.Show(new TranslationHint(HintTranslations.MaxAmmoReached, new HintParameter[2]
				{
					new AmmoHintParameter((byte)this._ammoType),
					new PackedULongHintParameter(this.MaxAmmo)
				}, HintEffectPresets.FadeInAndOut(0.25f), 1.5f));
			}
			this.CurrentAmmo += ammoAmount;
			PlayerEvents.OnPickedUpAmmo(new PlayerPickedUpAmmoEventArgs(base.Hub, this._ammoType, ammoAmount, base.TargetPickup as AmmoPickup));
		}
	}
}
