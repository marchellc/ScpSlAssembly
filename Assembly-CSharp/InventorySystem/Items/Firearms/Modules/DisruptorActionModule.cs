using System;
using System.Collections.Generic;
using Footprinting;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules;

public class DisruptorActionModule : ModuleBase, IReloaderModule, IActionModule, IBusyIndicatorModule
{
	private enum MessageType
	{
		RpcRequireReloadTrue,
		RpcRequireReloadFalse,
		RpcRequireReloadFullResync,
		RpcStartFiring,
		RpcStopFiring,
		RpcOnShot,
		CmdRequestStartFiring,
		CmdConfirmDischarge
	}

	public enum FiringState
	{
		None,
		FiringRapid,
		FiringSingle
	}

	private const float ShotTicketTimeout = 0.07f;

	private static readonly int FireSingleNormalHash = Animator.StringToHash("FireSingle");

	private static readonly int FireSingleLastHash = Animator.StringToHash("FireSingleLast");

	private static readonly int FireRapidNormalHash = Animator.StringToHash("FireRapid");

	private static readonly int FireRapidLastHash = Animator.StringToHash("FireRapidLast");

	private static readonly int StopFireHash = Animator.StringToHash("StopFire");

	private static readonly HashSet<ushort> ReloadRequiredSerials = new HashSet<ushort>();

	private readonly FullAutoShotsQueue<ShotBacktrackData> _receivedShots = new FullAutoShotsQueue<ShotBacktrackData>((ShotBacktrackData x) => x);

	private readonly Queue<double> _shotTickets = new Queue<double>();

	private MagazineModule _magModule;

	private DisruptorModeSelector _modeSelector;

	private DisruptorAudioModule _audioModule;

	private FiringState _curFiringState;

	private FiringState _lastActiveFiringState;

	private float _firingElapsed;

	[SerializeField]
	private float[] _singleShotTimes;

	[SerializeField]
	private float[] _rapidShotTimes;

	public FiringState CurFiringState
	{
		get
		{
			return this._curFiringState;
		}
		private set
		{
			this._curFiringState = value;
			if (value != FiringState.None)
			{
				this._lastActiveFiringState = value;
			}
			else
			{
				this._firingElapsed = 0f;
			}
		}
	}

	public bool IsFiring => this.CurFiringState != FiringState.None;

	public bool IsReloading => this.GetDisplayReloadingOrUnloading(base.ItemSerial);

	public bool IsUnloading => false;

	public bool IsLoaded
	{
		get
		{
			if (this._magModule.AmmoStored <= 0)
			{
				return this.CurFiringState != FiringState.None;
			}
			return true;
		}
	}

	public bool IsBusy
	{
		get
		{
			if (!this.IsFiring)
			{
				return this.IsReloading;
			}
			return true;
		}
	}

	[field: SerializeField]
	public float DisplayCyclicRate { get; private set; }

	[ExposedFirearmEvent]
	public void EndShootingAnimation()
	{
		this.CurFiringState = FiringState.None;
		DisruptorActionModule.ReloadRequiredSerials.Add(base.ItemSerial);
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartReloadOrUnload);
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.RpcRequireReloadTrue);
			});
		}
	}

	[ExposedFirearmEvent]
	public void FinishReloading()
	{
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.RpcRequireReloadFalse);
			});
		}
	}

	public bool GetDisplayReloadingOrUnloading(ushort serial)
	{
		return DisruptorActionModule.ReloadRequiredSerials.Contains(serial);
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((MessageType)reader.ReadByte())
		{
		case MessageType.RpcRequireReloadFullResync:
			DisruptorActionModule.ReloadRequiredSerials.Clear();
			while (reader.Remaining > 0)
			{
				DisruptorActionModule.ReloadRequiredSerials.Add(reader.ReadUShort());
			}
			break;
		case MessageType.RpcRequireReloadTrue:
			DisruptorActionModule.ReloadRequiredSerials.Add(serial);
			break;
		case MessageType.RpcRequireReloadFalse:
		case MessageType.RpcStopFiring:
			DisruptorActionModule.ReloadRequiredSerials.Remove(serial);
			break;
		case MessageType.RpcOnShot:
		{
			FiringState state = (FiringState)reader.ReadByte();
			ShotEventManager.Trigger(new DisruptorShotEvent(new ItemIdentifier(base.Firearm.ItemTypeId, serial), default(Footprint), state));
			break;
		}
		case MessageType.RpcStartFiring:
			break;
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		switch ((MessageType)reader.ReadByte())
		{
		case MessageType.RpcStartFiring:
			if (!base.IsLocalPlayer || base.IsServer)
			{
				bool singleMode = reader.ReadBool();
				bool last = reader.ReadBool();
				this.StartFiring(singleMode, last);
			}
			break;
		case MessageType.RpcStopFiring:
			base.Firearm.AnimSetTrigger(DisruptorActionModule.StopFireHash);
			this.CurFiringState = FiringState.None;
			this._audioModule.StopDisruptorShot();
			break;
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		switch ((MessageType)reader.ReadByte())
		{
		case MessageType.CmdRequestStartFiring:
			this.ServerProcessStartCmd(reader.ReadBool());
			break;
		case MessageType.CmdConfirmDischarge:
			this._receivedShots.Enqueue(new ShotBacktrackData(reader));
			this.ServerUpdateShotRequests();
			break;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		if (!base.Firearm.TryGetModules<MagazineModule, DisruptorModeSelector, DisruptorAudioModule>(out this._magModule, out this._modeSelector, out this._audioModule))
		{
			throw new NotImplementedException("The DisruptorActionModule does not implement all modules necessary for its operation.");
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		this.UpdateAction();
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.Reload, this.IsReloading);
		if (base.IsControllable)
		{
			this.ClientUpdate();
		}
		if (base.IsServer)
		{
			this.ServerUpdateShotRequests();
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		DisruptorActionModule.ReloadRequiredSerials.Clear();
	}

	internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
	{
		base.ServerOnPlayerConnected(hub, firstModule);
		if (firstModule)
		{
			this.SendRpc(hub, delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.RpcRequireReloadFullResync);
				DisruptorActionModule.ReloadRequiredSerials.ForEach(x.WriteUShort);
			});
		}
	}

	internal override void OnRemoved(ItemPickupBase pickupBase)
	{
		base.OnRemoved(pickupBase);
		if (!(pickupBase is FirearmPickup firearmPickup) || pickupBase == null || !this.TryGetCurStateTimes(out var times) || !firearmPickup.Worldmodel.TryGetExtension<DisruptorWorldmodelActionExtension>(out var extension))
		{
			return;
		}
		float[] array = new float[times.Length];
		for (int i = 0; i < times.Length; i++)
		{
			array[i] = times[i] - this._firingElapsed;
		}
		extension.ServerScheduleShot(new Footprint(base.Firearm.Owner), this.CurFiringState, array);
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.RpcRequireReloadTrue);
			});
		}
	}

	private void UpdateAction()
	{
		if (!this.TryGetCurStateTimes(out var times))
		{
			return;
		}
		float num = this._firingElapsed + Time.deltaTime;
		float[] array = times;
		foreach (float num2 in array)
		{
			if (!(num2 <= this._firingElapsed) && !(num2 > num))
			{
				this.TriggerFire();
			}
		}
		this._firingElapsed = num;
	}

	private bool TryGetCurStateTimes(out float[] times)
	{
		switch (this.CurFiringState)
		{
		case FiringState.FiringRapid:
			times = this._rapidShotTimes;
			return true;
		case FiringState.FiringSingle:
			times = this._singleShotTimes;
			return true;
		default:
			times = null;
			return false;
		}
	}

	private void TriggerFire()
	{
		if (base.IsServer)
		{
			this._shotTickets.Enqueue(NetworkTime.time);
			this.ServerUpdateShotRequests();
		}
		if (base.IsLocalPlayer)
		{
			ShotEventManager.Trigger(new DisruptorShotEvent(base.Firearm, this.CurFiringState));
			this.SendCmd(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(MessageType.CmdConfirmDischarge);
				writer.WriteBacktrackData(new ShotBacktrackData(base.Firearm));
			});
		}
	}

	private void ServerUpdateShotRequests()
	{
		if (!this._shotTickets.TryPeek(out var result))
		{
			return;
		}
		if (this._receivedShots.TryDequeue(out var dequeued))
		{
			dequeued.ProcessShot(base.Firearm, ServerFire);
		}
		else
		{
			if (NetworkTime.time - result < 0.07000000029802322)
			{
				return;
			}
			this.ServerFire(null);
		}
		this._shotTickets.Dequeue();
	}

	private void ServerProcessStartCmd(bool ads)
	{
		if (this.IsReloading || this.CurFiringState != FiringState.None || this._magModule.AmmoStored == 0)
		{
			return;
		}
		PlayerShootingWeaponEventArgs e = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
		PlayerEvents.OnShootingWeapon(e);
		if (!e.IsAllowed)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.RpcStopFiring);
			});
			return;
		}
		bool last = this._magModule.AmmoStored == 1;
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteSubheader(MessageType.RpcStartFiring);
			x.WriteBool(ads);
			x.WriteBool(last);
		});
		this._magModule.ServerModifyAmmo(-1);
		PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
	}

	private void ServerFire(ReferenceHub primaryTarget)
	{
		if (base.Firearm.TryGetModule<IHitregModule>(out var module))
		{
			module.Fire(primaryTarget, new DisruptorShotEvent(base.Firearm, this._lastActiveFiringState));
		}
		this.SendRpc((ReferenceHub hub) => hub != base.Firearm.Owner, delegate(NetworkWriter x)
		{
			x.WriteSubheader(MessageType.RpcOnShot);
			x.WriteByte((byte)this._lastActiveFiringState);
		});
	}

	private void ClientUpdate()
	{
		if (!base.Firearm.AnyModuleBusy() && base.GetAction(ActionName.Shoot) && !base.PrimaryActionBlocked && !this.IsReloading && this.CurFiringState == FiringState.None)
		{
			if (!base.IsServer)
			{
				this.StartFiring(this._modeSelector.SingleShotSelected, this._magModule.AmmoStored <= 1);
			}
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.CmdRequestStartFiring);
				x.WriteBool(this._modeSelector.SingleShotSelected);
			});
		}
	}

	private void StartFiring(bool singleMode, bool last)
	{
		if (singleMode)
		{
			this.CurFiringState = FiringState.FiringSingle;
			base.Firearm.AnimSetTrigger(last ? DisruptorActionModule.FireSingleLastHash : DisruptorActionModule.FireSingleNormalHash);
		}
		else
		{
			this.CurFiringState = FiringState.FiringRapid;
			base.Firearm.AnimSetTrigger(last ? DisruptorActionModule.FireRapidLastHash : DisruptorActionModule.FireRapidNormalHash);
		}
		this._audioModule.PlayDisruptorShot(singleMode, last);
	}
}
