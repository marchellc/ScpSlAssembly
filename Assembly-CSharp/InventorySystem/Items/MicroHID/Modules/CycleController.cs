using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules;

public class CycleController
{
	private const float MinIdleCooldown = 1.5f;

	private float _windUpProgress;

	private float _serverIdleTimer;

	private bool _hasOwner;

	private MicroHidPhase _phase;

	private MicroHIDItem _prevItem;

	private readonly List<FiringModeControllerModule> _firingModeControllers = new List<FiringModeControllerModule>();

	private readonly Dictionary<MicroHidPhase, double> _lastChangeTimes = new Dictionary<MicroHidPhase, double>();

	public readonly ushort Serial;

	public float ServerWindUpProgress
	{
		get
		{
			return this._windUpProgress;
		}
		private set
		{
			this._windUpProgress = Mathf.Clamp01(value);
		}
	}

	public float CurrentPhaseElapsed
	{
		get
		{
			if (!this.TryGetElapsed(this.Phase, out var elapsed))
			{
				return Time.timeSinceLevelLoad;
			}
			return elapsed;
		}
	}

	public MicroHidPhase Phase
	{
		get
		{
			return this._phase;
		}
		set
		{
			if (this._phase != value)
			{
				this._phase = value;
				this._lastChangeTimes[value] = NetworkTime.time;
				CycleController.OnPhaseChanged?.Invoke(this.Serial, value);
			}
		}
	}

	public MicroHidFiringMode LastFiringMode { get; set; }

	public static event Action<ushort, MicroHidPhase> OnPhaseChanged;

	public CycleController(ushort serial)
	{
		this.Serial = serial;
	}

	public void ServerUpdateHeldItem(MicroHIDItem item)
	{
		if (item.ItemSerial != this.Serial)
		{
			throw new ArgumentException("The provided held MicroHID has a serial number that does not match the controller.", "item");
		}
		if (item != this._prevItem)
		{
			this.RecacheFiringModes(item);
			this._prevItem = item;
		}
		this._hasOwner = true;
		this.UpdatePhase();
	}

	public void ServerUpdatePickup(MicroHIDPickup pickup)
	{
		PickupSyncInfo info = pickup.Info;
		if (info.Serial != this.Serial)
		{
			throw new ArgumentException("The provided MicroHID pickup has a serial number that does not match the controller.", "pickup");
		}
		if (this._hasOwner)
		{
			this._hasOwner = false;
			this._prevItem = null;
			this.RecacheFiringModes(info.ItemId.GetTemplate<MicroHIDItem>());
		}
		this.UpdatePhase();
	}

	public bool TryGetElapsed(MicroHidPhase phase, out float elapsed)
	{
		if (this._lastChangeTimes.TryGetValue(phase, out var value))
		{
			elapsed = (float)(NetworkTime.time - value);
			return true;
		}
		elapsed = 0f;
		return false;
	}

	public bool TryGetLastFiringController(out FiringModeControllerModule ret)
	{
		if (!NetworkServer.active && this._firingModeControllers.Count == 0)
		{
			this.RecacheFiringModes(ItemType.MicroHID.GetTemplate<MicroHIDItem>());
		}
		foreach (FiringModeControllerModule firingModeController in this._firingModeControllers)
		{
			if (firingModeController.AssignedMode == this.LastFiringMode)
			{
				ret = firingModeController;
				return true;
			}
		}
		ret = null;
		return false;
	}

	private void RecacheFiringModes(MicroHIDItem item)
	{
		this._firingModeControllers.Clear();
		SubcomponentBase[] allSubcomponents = item.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			if (allSubcomponents[i] is FiringModeControllerModule item2)
			{
				this._firingModeControllers.Add(item2);
			}
		}
	}

	private void UpdatePhase()
	{
		if (!NetworkServer.active)
		{
			throw new InvalidOperationException("Attempting to update phase as a client");
		}
		switch (this.Phase)
		{
		case MicroHidPhase.Standby:
			this.UpdateStandby();
			return;
		case MicroHidPhase.WindingUp:
			this.UpdateWindingUp();
			break;
		case MicroHidPhase.WindingDown:
			this.UpdateWindingDown();
			break;
		case MicroHidPhase.WoundUpSustain:
			this.UpdateWoundUp();
			break;
		case MicroHidPhase.Firing:
			this.UpdateFiring();
			break;
		default:
			throw new InvalidOperationException(string.Format("Unknown {0}: {1}", "MicroHidPhase", this.Phase));
		}
		if (this._hasOwner && this.TryGetLastFiringController(out var ret))
		{
			ret.ServerUpdateSelected(this.Phase);
		}
	}

	private void UpdateStandby()
	{
		if (this._serverIdleTimer < 1.5f)
		{
			this._serverIdleTimer += Time.deltaTime;
		}
		else
		{
			if (!this._hasOwner)
			{
				return;
			}
			foreach (FiringModeControllerModule firingModeController in this._firingModeControllers)
			{
				if (firingModeController.ValidateStart && firingModeController.ValidateUpdate)
				{
					this._serverIdleTimer = 0f;
					this.Phase = MicroHidPhase.WindingUp;
					this.LastFiringMode = firingModeController.AssignedMode;
					break;
				}
			}
		}
	}

	private void UpdateWindingUp()
	{
		if (this.TryGetLastFiringController(out var ret))
		{
			this.ServerWindUpProgress += ret.WindUpRate * Time.deltaTime;
			if (!this._hasOwner || !ret.ValidateUpdate)
			{
				this.Phase = MicroHidPhase.WindingDown;
			}
			else if (this.ServerWindUpProgress >= 1f)
			{
				this.Phase = (ret.ValidateEnterFire ? MicroHidPhase.Firing : MicroHidPhase.WoundUpSustain);
			}
		}
	}

	private void UpdateWindingDown()
	{
		if (this.TryGetLastFiringController(out var ret))
		{
			this.ServerWindUpProgress -= ret.WindDownRate * Time.deltaTime;
			if (this.ServerWindUpProgress <= 0f)
			{
				this.Phase = MicroHidPhase.Standby;
			}
		}
	}

	private void UpdateWoundUp()
	{
		if (this.TryGetLastFiringController(out var ret))
		{
			if (!this._hasOwner || !ret.ValidateUpdate)
			{
				this.Phase = MicroHidPhase.WindingDown;
			}
			else if (ret.ValidateEnterFire)
			{
				this.Phase = MicroHidPhase.Firing;
			}
		}
	}

	private void UpdateFiring()
	{
		if (this.TryGetLastFiringController(out var ret) && (!this._hasOwner || !ret.ValidateUpdate))
		{
			this.Phase = MicroHidPhase.WindingDown;
		}
	}
}
