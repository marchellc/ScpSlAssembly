using System;
using Hints;
using InventorySystem.Configs;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using UnityEngine;

namespace InventorySystem.Searching
{
	public class AmmoSearchCompletor : SearchCompletor
	{
		private ushort CurrentAmmo
		{
			get
			{
				return this.Hub.inventory.GetCurAmmo(this._ammoType);
			}
			set
			{
				this.Hub.inventory.ServerSetAmmo(this._ammoType, (int)value);
			}
		}

		private ushort MaxAmmo
		{
			get
			{
				return InventoryLimits.GetAmmoLimit(this._ammoType, this.Hub);
			}
		}

		public AmmoSearchCompletor(ReferenceHub hub, ItemPickupBase targetPickup, ItemBase targetItem, double maxDistanceSquared)
			: base(hub, targetPickup, targetItem, maxDistanceSquared)
		{
			this._ammoType = targetItem.ItemTypeId;
		}

		public override bool ValidateStart()
		{
			if (!base.ValidateStart())
			{
				return false;
			}
			PlayerSearchingAmmoEventArgs playerSearchingAmmoEventArgs = new PlayerSearchingAmmoEventArgs(this.Hub, this.TargetPickup);
			PlayerEvents.OnSearchingAmmo(playerSearchingAmmoEventArgs);
			return playerSearchingAmmoEventArgs.IsAllowed;
		}

		protected override bool ValidateAny()
		{
			if (!base.ValidateAny())
			{
				return false;
			}
			uint maxAmmo = (uint)this.MaxAmmo;
			if ((uint)this.CurrentAmmo >= maxAmmo)
			{
				this.Hub.hints.Show(new TranslationHint(HintTranslations.MaxAmmoAlreadyReached, new HintParameter[]
				{
					new AmmoHintParameter((byte)this._ammoType),
					new PackedULongHintParameter((ulong)maxAmmo)
				}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 2) }, 2f));
				return false;
			}
			return true;
		}

		public override void Complete()
		{
			AmmoPickup ammoPickup = this.TargetPickup as AmmoPickup;
			if (ammoPickup == null)
			{
				Debug.LogError("The pickup needs to derive from AmmoPickup");
				return;
			}
			ushort currentAmmo = this.CurrentAmmo;
			ushort num = (ushort)(Math.Min((int)(currentAmmo + ammoPickup.SavedAmmo), (int)this.MaxAmmo) - (int)currentAmmo);
			PlayerPickingUpAmmoEventArgs playerPickingUpAmmoEventArgs = new PlayerPickingUpAmmoEventArgs(this.Hub, this._ammoType, num, this.TargetPickup);
			PlayerEvents.OnPickingUpAmmo(playerPickingUpAmmoEventArgs);
			if (!playerPickingUpAmmoEventArgs.IsAllowed)
			{
				return;
			}
			num = playerPickingUpAmmoEventArgs.AmmoAmount;
			if (num >= ammoPickup.SavedAmmo)
			{
				this.TargetPickup.DestroySelf();
			}
			else
			{
				AmmoPickup ammoPickup2 = ammoPickup;
				ammoPickup2.NetworkSavedAmmo = ammoPickup2.SavedAmmo - num;
				PickupSyncInfo info = this.TargetPickup.Info;
				info.InUse = false;
				this.TargetPickup.NetworkInfo = info;
				this.Hub.hints.Show(new TranslationHint(HintTranslations.MaxAmmoReached, new HintParameter[]
				{
					new AmmoHintParameter((byte)this._ammoType),
					new PackedULongHintParameter((ulong)this.MaxAmmo)
				}, HintEffectPresets.FadeInAndOut(0.25f, 1f, 0f), 1.5f));
			}
			this.CurrentAmmo += num;
			PlayerEvents.OnPickedUpAmmo(new PlayerPickedUpAmmoEventArgs(this.Hub, this._ammoType, num, this.TargetPickup));
		}

		private readonly ItemType _ammoType;
	}
}
