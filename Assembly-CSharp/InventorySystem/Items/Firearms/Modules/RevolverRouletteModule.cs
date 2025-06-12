using System;
using InventorySystem.Items.Firearms.Modules.Misc;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class RevolverRouletteModule : ModuleBase, IBusyIndicatorModule, IAdsPreventerModule
{
	private readonly ClientRequestTimer _requestTimer = new ClientRequestTimer();

	private CylinderAmmoModule _cylinderModule;

	private DoubleActionModule _doubleActionModule;

	private float _keyHoldTime;

	private bool _busy;

	public bool IsBusy
	{
		get
		{
			if (!this._busy)
			{
				return this._requestTimer.Busy;
			}
			return true;
		}
	}

	public bool AdsAllowed => !this._busy;

	protected override void OnInit()
	{
		base.OnInit();
		if (!base.Firearm.TryGetModules<DoubleActionModule, CylinderAmmoModule>(out this._doubleActionModule, out this._cylinderModule))
		{
			throw new InvalidOperationException("The " + base.Firearm.name + " is missing one or more essential modules (required by " + base.name + ").");
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		this._busy = false;
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!base.IsControllable || base.PrimaryActionBlocked)
		{
			return;
		}
		if (!base.GetAction(ActionName.WeaponAlt) || base.Firearm.AnyModuleBusy())
		{
			this._keyHoldTime = 0f;
			return;
		}
		this._keyHoldTime += Time.deltaTime;
		if (this._keyHoldTime > 1f)
		{
			this._requestTimer.Trigger();
			this.SendCmd();
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (base.IsLocalPlayer || !base.Firearm.AnyModuleBusy())
		{
			this.SendRpc();
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		this._busy = true;
		if (this._doubleActionModule.Cocked)
		{
			this._doubleActionModule.TriggerDecocking(FirearmAnimatorHashes.Roulette);
		}
		else
		{
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Roulette);
		}
	}

	[ExposedFirearmEvent]
	public void ServerRandomize()
	{
		if (base.IsServer)
		{
			int ammoMax = this._cylinderModule.AmmoMax;
			int rotations = UnityEngine.Random.Range(0, ammoMax);
			this._cylinderModule.RotateCylinder(rotations);
		}
	}

	[ExposedFirearmEvent]
	public void EndSpin()
	{
		this._busy = false;
	}
}
