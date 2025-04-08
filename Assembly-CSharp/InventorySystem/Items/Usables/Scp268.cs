using System;
using System.Diagnostics;
using CustomPlayerEffects;
using InventorySystem.Drawers;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

namespace InventorySystem.Items.Usables
{
	public class Scp268 : UsableItem, IWearableItem
	{
		public override AlertContent Alert
		{
			get
			{
				if (!this.IsWorn)
				{
					return base.Alert;
				}
				return default(AlertContent);
			}
		}

		public bool IsWorn
		{
			get
			{
				if (!this.IsLocalPlayer || NetworkServer.active)
				{
					return this._isWorn;
				}
				return base.Owner.playerEffectsController.GetEffect<Invisible>().Intensity > 0;
			}
			set
			{
				this._isWorn = value;
				if (!NetworkServer.active)
				{
					return;
				}
				if (value)
				{
					base.Owner.EnableWearables(WearableElements.Scp268Hat);
					return;
				}
				base.Owner.DisableWearables(WearableElements.Scp268Hat);
			}
		}

		public WearableSlot Slot
		{
			get
			{
				return WearableSlot.Hat;
			}
		}

		public override bool AllowHolster
		{
			get
			{
				return !this.IsUsing || this.IsWorn;
			}
		}

		private Invisible Effect
		{
			get
			{
				return base.Owner.playerEffectsController.GetEffect<Invisible>();
			}
		}

		public override void ServerOnUsingCompleted()
		{
			this.IsUsing = false;
			this.IsWorn = true;
			this.SetState(true);
			base.ServerSetPersonalCooldown(120f);
		}

		public override void OnHolstered()
		{
			base.OnHolstered();
			if (NetworkServer.active)
			{
				this.SetState(false);
			}
			if (this.IsLocalPlayer)
			{
				this.IsUsing = false;
			}
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			if (this.IsLocalPlayer && this.IsWorn && this.IsUsing)
			{
				this.IsUsing = false;
			}
			if (!NetworkServer.active || !this._stopwatch.IsRunning)
			{
				return;
			}
			if (this._stopwatch.Elapsed.TotalSeconds >= 15.0 || this.Effect.Intensity == 0)
			{
				this.SetState(false);
			}
		}

		private void SetState(bool state)
		{
			if (state)
			{
				this.Effect.Intensity = 1;
				this._stopwatch.Restart();
				return;
			}
			if (this.IsWorn)
			{
				this.Effect.Intensity = 0;
				this._stopwatch.Stop();
				this.IsWorn = false;
				if (base.OwnerInventory.CurItem.TypeId == this.ItemTypeId)
				{
					base.OwnerInventory.ServerSelectItem(0);
				}
			}
		}

		private const float InvisibilityTime = 15f;

		private const float CooldownTime = 120f;

		private readonly Stopwatch _stopwatch = new Stopwatch();

		private bool _isWorn;
	}
}
