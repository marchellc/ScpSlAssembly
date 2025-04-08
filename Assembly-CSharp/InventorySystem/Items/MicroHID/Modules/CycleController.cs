using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.MicroHID.Modules
{
	public class CycleController
	{
		public static event Action<ushort, MicroHidPhase> OnPhaseChanged;

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
				float num;
				if (!this.TryGetElapsed(this.Phase, out num))
				{
					return Time.timeSinceLevelLoad;
				}
				return num;
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
				if (this._phase == value)
				{
					return;
				}
				this._phase = value;
				this._lastChangeTimes[value] = NetworkTime.time;
				Action<ushort, MicroHidPhase> onPhaseChanged = CycleController.OnPhaseChanged;
				if (onPhaseChanged == null)
				{
					return;
				}
				onPhaseChanged(this.Serial, value);
			}
		}

		public MicroHidFiringMode LastFiringMode { get; set; }

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
			double num;
			if (this._lastChangeTimes.TryGetValue(phase, out num))
			{
				elapsed = (float)(NetworkTime.time - num);
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
			foreach (FiringModeControllerModule firingModeControllerModule in this._firingModeControllers)
			{
				if (firingModeControllerModule.AssignedMode == this.LastFiringMode)
				{
					ret = firingModeControllerModule;
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
				FiringModeControllerModule firingModeControllerModule = allSubcomponents[i] as FiringModeControllerModule;
				if (firingModeControllerModule != null)
				{
					this._firingModeControllers.Add(firingModeControllerModule);
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
			FiringModeControllerModule firingModeControllerModule;
			if (this._hasOwner && this.TryGetLastFiringController(out firingModeControllerModule))
			{
				firingModeControllerModule.ServerUpdateSelected(this.Phase);
			}
		}

		private void UpdateStandby()
		{
			if (this._serverIdleTimer < 1.5f)
			{
				this._serverIdleTimer += Time.deltaTime;
				return;
			}
			if (!this._hasOwner)
			{
				return;
			}
			foreach (FiringModeControllerModule firingModeControllerModule in this._firingModeControllers)
			{
				if (firingModeControllerModule.ValidateStart && firingModeControllerModule.ValidateUpdate)
				{
					this._serverIdleTimer = 0f;
					this.Phase = MicroHidPhase.WindingUp;
					this.LastFiringMode = firingModeControllerModule.AssignedMode;
					break;
				}
			}
		}

		private void UpdateWindingUp()
		{
			FiringModeControllerModule firingModeControllerModule;
			if (!this.TryGetLastFiringController(out firingModeControllerModule))
			{
				return;
			}
			this.ServerWindUpProgress += firingModeControllerModule.WindUpRate * Time.deltaTime;
			if (!this._hasOwner || !firingModeControllerModule.ValidateUpdate)
			{
				this.Phase = MicroHidPhase.WindingDown;
				return;
			}
			if (this.ServerWindUpProgress >= 1f)
			{
				this.Phase = (firingModeControllerModule.ValidateEnterFire ? MicroHidPhase.Firing : MicroHidPhase.WoundUpSustain);
			}
		}

		private void UpdateWindingDown()
		{
			FiringModeControllerModule firingModeControllerModule;
			if (!this.TryGetLastFiringController(out firingModeControllerModule))
			{
				return;
			}
			this.ServerWindUpProgress -= firingModeControllerModule.WindDownRate * Time.deltaTime;
			if (this.ServerWindUpProgress <= 0f)
			{
				this.Phase = MicroHidPhase.Standby;
			}
		}

		private void UpdateWoundUp()
		{
			FiringModeControllerModule firingModeControllerModule;
			if (!this.TryGetLastFiringController(out firingModeControllerModule))
			{
				return;
			}
			if (!this._hasOwner || !firingModeControllerModule.ValidateUpdate)
			{
				this.Phase = MicroHidPhase.WindingDown;
				return;
			}
			if (firingModeControllerModule.ValidateEnterFire)
			{
				this.Phase = MicroHidPhase.Firing;
			}
		}

		private void UpdateFiring()
		{
			FiringModeControllerModule firingModeControllerModule;
			if (!this.TryGetLastFiringController(out firingModeControllerModule))
			{
				return;
			}
			if (this._hasOwner && firingModeControllerModule.ValidateUpdate)
			{
				return;
			}
			this.Phase = MicroHidPhase.WindingDown;
		}

		private const float MinIdleCooldown = 1.5f;

		private float _windUpProgress;

		private float _serverIdleTimer;

		private bool _hasOwner;

		private MicroHidPhase _phase;

		private MicroHIDItem _prevItem;

		private readonly List<FiringModeControllerModule> _firingModeControllers = new List<FiringModeControllerModule>();

		private readonly Dictionary<MicroHidPhase, double> _lastChangeTimes = new Dictionary<MicroHidPhase, double>();

		public readonly ushort Serial;
	}
}
