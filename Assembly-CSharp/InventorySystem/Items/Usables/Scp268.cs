using System.Diagnostics;
using CustomPlayerEffects;
using InventorySystem.Drawers;
using Mirror;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

namespace InventorySystem.Items.Usables;

public class Scp268 : UsableItem, IWearableItem
{
	private const float InvisibilityTime = 15f;

	private const float CooldownTime = 120f;

	private readonly Stopwatch _stopwatch = new Stopwatch();

	private bool _isWorn;

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
			return base.Owner.playerEffectsController.GetEffect<Invisible>().Intensity != 0;
		}
		set
		{
			this._isWorn = value;
			if (NetworkServer.active)
			{
				if (value)
				{
					base.Owner.EnableWearables(WearableElements.Scp268Hat);
				}
				else
				{
					base.Owner.DisableWearables(WearableElements.Scp268Hat);
				}
			}
		}
	}

	public WearableSlot Slot => WearableSlot.Hat;

	public override bool AllowHolster
	{
		get
		{
			if (base.IsUsing)
			{
				return this.IsWorn;
			}
			return true;
		}
	}

	private Invisible Effect => base.Owner.playerEffectsController.GetEffect<Invisible>();

	public override void ServerOnUsingCompleted()
	{
		base.IsUsing = false;
		this.IsWorn = true;
		this.SetState(state: true);
		base.ServerSetPersonalCooldown(120f);
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (NetworkServer.active)
		{
			this.SetState(state: false);
		}
		if (this.IsLocalPlayer)
		{
			base.IsUsing = false;
		}
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (this.IsLocalPlayer && this.IsWorn && base.IsUsing)
		{
			base.IsUsing = false;
		}
		if (NetworkServer.active && this._stopwatch.IsRunning && (this._stopwatch.Elapsed.TotalSeconds >= 15.0 || this.Effect.Intensity == 0))
		{
			this.SetState(state: false);
		}
	}

	private void SetState(bool state)
	{
		if (state)
		{
			this.Effect.Intensity = 1;
			this._stopwatch.Restart();
		}
		else if (this.IsWorn)
		{
			this.Effect.Intensity = 0;
			this._stopwatch.Stop();
			this.IsWorn = false;
			if (base.OwnerInventory.CurItem.TypeId == base.ItemTypeId)
			{
				base.OwnerInventory.ServerSelectItem(0);
			}
		}
	}
}
