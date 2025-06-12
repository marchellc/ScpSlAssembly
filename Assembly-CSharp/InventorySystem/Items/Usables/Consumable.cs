using System.Diagnostics;
using CustomPlayerEffects;
using InventorySystem.Drawers;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables;

public abstract class Consumable : UsableItem, IItemProgressbarDrawer, IItemDrawer
{
	[SerializeField]
	private float _activationTime;

	private float _realActivationTime;

	[SerializeField]
	private bool _showProgressBar;

	private readonly Stopwatch _useStopwatch = new Stopwatch();

	private bool _alreadyActivated;

	public bool ProgressbarEnabled
	{
		get
		{
			if (this._showProgressBar)
			{
				return !this.AllowHolster;
			}
			return false;
		}
	}

	public float ProgressbarMin => 0f;

	public float ProgressbarMax => this._realActivationTime;

	public float ProgressbarValue { get; private set; }

	public float ProgressbarWidth => 650f;

	public override bool AllowHolster
	{
		get
		{
			if (this._useStopwatch.IsRunning)
			{
				return this._useStopwatch.Elapsed.TotalSeconds >= (double)this._realActivationTime;
			}
			return true;
		}
	}

	private bool ActivationReady
	{
		get
		{
			if (NetworkServer.active && !this._alreadyActivated && this._useStopwatch.IsRunning)
			{
				return this._useStopwatch.Elapsed.TotalSeconds >= (double)this._realActivationTime;
			}
			return false;
		}
	}

	public override void OnEquipped()
	{
		base.OnEquipped();
		this._realActivationTime = this._activationTime;
	}

	public override void OnUsingStarted()
	{
		base.OnUsingStarted();
		this.ProgressbarValue = 0f;
		this._useStopwatch.Restart();
		this._realActivationTime = this._activationTime;
		if (base.ItemTypeId.TryGetSpeedMultiplier(base.Owner, out var multiplier) && multiplier != 0f)
		{
			this._realActivationTime /= multiplier;
		}
	}

	public override void OnUsingCancelled()
	{
		base.OnUsingCancelled();
		this._useStopwatch.Stop();
		this._realActivationTime = this._activationTime;
	}

	public override void ServerOnUsingCompleted()
	{
		base.OwnerInventory.NetworkCurItem = ItemIdentifier.None;
		base.OwnerInventory.CurInstance = null;
		if (!this._alreadyActivated)
		{
			this.ActivateEffects();
		}
		base.ServerRemoveSelf();
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (this.IsLocalPlayer && this.ProgressbarEnabled)
		{
			this.ProgressbarValue += Time.deltaTime;
		}
		if (this.ActivationReady)
		{
			this.ActivateEffects();
		}
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		if (NetworkServer.active && this._alreadyActivated)
		{
			base.ServerRemoveSelf();
		}
	}

	public override void OnRemoved(ItemPickupBase pickup)
	{
		base.OnRemoved(pickup);
		if (this.ActivationReady)
		{
			this.ActivateEffects();
		}
		if (this._alreadyActivated && pickup != null)
		{
			pickup.DestroySelf();
		}
		if (NetworkServer.active)
		{
			UsableItemsController.GetHandler(base.Owner).CurrentUsable = CurrentlyUsedItem.None;
		}
	}

	private void ActivateEffects()
	{
		if (NetworkServer.active)
		{
			this.OnEffectsActivated();
			this._alreadyActivated = true;
		}
	}

	protected abstract void OnEffectsActivated();
}
