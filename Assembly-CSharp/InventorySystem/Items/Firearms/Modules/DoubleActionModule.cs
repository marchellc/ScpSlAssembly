using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;
using Utils;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules;

public class DoubleActionModule : ModuleBase, IActionModule, IBusyIndicatorModule, IInspectPreventerModule
{
	private enum MessageType : byte
	{
		RpcNewPlayerResync,
		RpcSetCockedTrue,
		RpcSetCockedFalse,
		RpcFire,
		RpcDryFire,
		CmdUpdatePulling,
		CmdShoot,
		StartPulling,
		StartCocking
	}

	private class TriggerPull
	{
		private double _pullStartTime;

		private double _pullEndTime;

		private double InverseLerpTime => MoreMath.InverseLerp(this._pullStartTime, this._pullEndTime, NetworkTime.time);

		public float PullProgress
		{
			get
			{
				if (!this.IsPulling)
				{
					return 0f;
				}
				return (float)this.InverseLerpTime;
			}
		}

		public bool IsPulling { get; private set; }

		public void Pull(float durationSeconds)
		{
			this.IsPulling = true;
			this._pullStartTime = NetworkTime.time;
			this._pullEndTime = NetworkTime.time + (double)durationSeconds;
		}

		public void Reset()
		{
			this.IsPulling = false;
		}
	}

	private static readonly HashSet<ushort> CockedHashes = new HashSet<ushort>();

	private const float CockingKeyMaxHoldTime = 0.5f;

	[SerializeField]
	private float _baseDoubleActionTime;

	[SerializeField]
	private float _basePostShotCooldown;

	[SerializeField]
	private AudioClip[] _fireClips;

	[SerializeField]
	private AudioClip _dryFireClip;

	[SerializeField]
	private AudioClip _doubleActionClip;

	[SerializeField]
	private AudioClip _cockingClip;

	[SerializeField]
	private AudioClip _decockingClip;

	private float _cockingKeyHoldTime;

	private int? _afterDecockTrigger;

	private bool _cockingBusy;

	private bool _serverPullTokenUsed;

	private ClientPredictedValue<bool> _clientCocked;

	private AudioModule _audioModule;

	private CylinderAmmoModule _cylinderModule;

	private IHitregModule _hitregModule;

	private readonly TriggerPull _triggerPull = new TriggerPull();

	private readonly ClientRequestTimer _cockRequestTimer = new ClientRequestTimer();

	private readonly FullAutoRateLimiter _clientShotCooldown = new FullAutoRateLimiter();

	private readonly FullAutoRateLimiter _serverShotCooldown = new FullAutoRateLimiter();

	public bool IsBusy
	{
		get
		{
			if (!this.OnCooldown && !this.IsCocking)
			{
				return this._triggerPull.IsPulling;
			}
			return true;
		}
	}

	public bool InspectionAllowed
	{
		get
		{
			if (this._cylinderModule.AmmoStored != 0)
			{
				return !this.IsBusy;
			}
			return !this.IsCocking;
		}
	}

	public bool Cocked
	{
		get
		{
			if (!base.IsServer)
			{
				return this._clientCocked.Value;
			}
			return DoubleActionModule.GetCocked(base.ItemSerial);
		}
		private set
		{
			if (!base.IsServer)
			{
				this._clientCocked.Value = value;
			}
			else if (value)
			{
				DoubleActionModule.CockedHashes.Add(base.ItemSerial);
				this.SendRpc(MessageType.RpcSetCockedTrue);
			}
			else
			{
				DoubleActionModule.CockedHashes.Remove(base.ItemSerial);
				this.SendRpc(MessageType.RpcSetCockedFalse);
			}
		}
	}

	public float TriggerPullProgress => this._triggerPull.PullProgress;

	public float DisplayCyclicRate => 1f / (this.DoubleActionTime + this.TimeBetweenShots);

	public bool IsLoaded => this._cylinderModule.AmmoStored > 0;

	private float DoubleActionTime => this._baseDoubleActionTime / base.Firearm.AttachmentsValue(AttachmentParam.DoubleActionSpeedMultiplier);

	private float TimeBetweenShots => this._basePostShotCooldown / base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);

	private bool OnCooldown
	{
		get
		{
			if (this._clientShotCooldown.Ready)
			{
				return !this._serverShotCooldown.Ready;
			}
			return true;
		}
	}

	private bool IsCocking
	{
		get
		{
			if (!this._cockingBusy)
			{
				return this._cockRequestTimer.Busy;
			}
			return true;
		}
	}

	public static event Action<ushort, bool> OnCockedChanged;

	public static bool GetCocked(ushort serial)
	{
		return DoubleActionModule.CockedHashes.Contains(serial);
	}

	public void TriggerDecocking(int? nextTriggerHash = null)
	{
		this._audioModule.PlayQuiet(this._decockingClip);
		this._cockingBusy = true;
		this._afterDecockTrigger = nextTriggerHash;
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.DeCock);
	}

	public void TriggerCocking()
	{
		this._audioModule.PlayNormal(this._cockingClip);
		this._cockingBusy = true;
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Cock);
		if (base.IsLocalPlayer)
		{
			this._cylinderModule.ClientHoldPrediction();
		}
	}

	[ExposedFirearmEvent]
	public void SetCocked(bool value)
	{
		this.Cocked = value;
	}

	[ExposedFirearmEvent]
	public void OnDecockingAnimComplete()
	{
		this._cockingBusy = false;
		if (this._afterDecockTrigger.HasValue)
		{
			base.Firearm.AnimSetTrigger(this._afterDecockTrigger.Value);
			this._afterDecockTrigger = null;
		}
	}

	[ExposedFirearmEvent]
	public void OnCockingAnimComplete()
	{
		this._cockingBusy = false;
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch (this.DecodeHeader(reader))
		{
		case MessageType.RpcNewPlayerResync:
			foreach (ushort cockedHash in DoubleActionModule.CockedHashes)
			{
				DoubleActionModule.OnCockedChanged?.Invoke(cockedHash, arg2: false);
			}
			DoubleActionModule.CockedHashes.Clear();
			DoubleActionModule.CockedHashes.EnsureCapacity(reader.Remaining / 2);
			while (reader.Remaining > 0)
			{
				ushort num = reader.ReadUShort();
				DoubleActionModule.CockedHashes.Add(num);
				DoubleActionModule.OnCockedChanged?.Invoke(num, arg2: true);
			}
			break;
		case MessageType.RpcSetCockedTrue:
			DoubleActionModule.CockedHashes.Add(serial);
			DoubleActionModule.OnCockedChanged?.Invoke(serial, arg2: true);
			break;
		case MessageType.RpcSetCockedFalse:
			DoubleActionModule.CockedHashes.Remove(serial);
			DoubleActionModule.OnCockedChanged?.Invoke(serial, arg2: false);
			break;
		case MessageType.RpcFire:
			ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm.ItemTypeId, serial)));
			break;
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		switch (this.DecodeHeader(reader))
		{
		case MessageType.StartCocking:
			this.TriggerCocking();
			break;
		case MessageType.StartPulling:
			if (base.IsSpectator)
			{
				this._triggerPull.Pull(this.DoubleActionTime);
			}
			break;
		case MessageType.RpcFire:
			if (base.IsSpectator)
			{
				this.PlayFireAnims(FirearmAnimatorHashes.Fire);
				this._triggerPull.Reset();
			}
			break;
		case MessageType.RpcDryFire:
			if (base.IsSpectator)
			{
				this.PlayFireAnims(FirearmAnimatorHashes.DryFire);
				this._triggerPull.Reset();
			}
			break;
		case MessageType.CmdUpdatePulling:
		case MessageType.CmdShoot:
			break;
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (base.PrimaryActionBlocked)
		{
			return;
		}
		switch (this.DecodeHeader(reader))
		{
		case MessageType.StartCocking:
			if (!base.Firearm.AnyModuleBusy(this) && !this.Cocked && !this._cockingBusy)
			{
				this.SendRpc(MessageType.StartCocking);
			}
			break;
		case MessageType.StartPulling:
			if (!this._serverPullTokenUsed)
			{
				this._serverPullTokenUsed = true;
				this._audioModule.PlayQuiet(this._doubleActionClip);
				double targetTime = NetworkTime.time + (double)this.DoubleActionTime;
				this.SendRpc(MessageType.StartPulling, delegate(NetworkWriter x)
				{
					x.WriteDouble(targetTime);
				});
			}
			break;
		case MessageType.CmdShoot:
			this.Fire(reader);
			this._cylinderModule.ServerResync();
			break;
		}
	}

	protected override void OnInit()
	{
		base.OnInit();
		this._clientCocked = new ClientPredictedValue<bool>(() => DoubleActionModule.GetCocked(base.ItemSerial));
		if (!base.Firearm.TryGetModules<AudioModule, CylinderAmmoModule, IHitregModule>(out this._audioModule, out this._cylinderModule, out this._hitregModule))
		{
			throw new InvalidOperationException("The " + base.Firearm.name + " is missing one or more essential modules (required by " + base.name + ").");
		}
		this._audioModule.RegisterClip(this._dryFireClip);
		this._audioModule.RegisterClip(this._doubleActionClip);
		this._audioModule.RegisterClip(this._cockingClip);
		this._audioModule.RegisterClip(this._decockingClip);
		AudioClip[] fireClips = this._fireClips;
		foreach (AudioClip clip in fireClips)
		{
			this._audioModule.RegisterClip(clip);
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		DoubleActionModule.CockedHashes.Clear();
	}

	internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
	{
		base.ServerOnPlayerConnected(hub, firstModule);
		if (!firstModule)
		{
			return;
		}
		this.SendRpc(hub, delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MessageType.RpcNewPlayerResync);
			DoubleActionModule.CockedHashes.ForEach(delegate(ushort x)
			{
				writer.WriteUShort(x);
			});
		});
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		this._cockingBusy = false;
		this._cockingKeyHoldTime = 0f;
		this._serverPullTokenUsed = false;
		this._triggerPull.Reset();
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		if (base.IsServer)
		{
			if (this.Cocked)
			{
				this.SendRpc(MessageType.RpcSetCockedTrue);
			}
			else
			{
				this.SendRpc(MessageType.RpcSetCockedFalse);
			}
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		this._clientShotCooldown.Update();
		this._serverShotCooldown.Update();
		if (base.IsControllable)
		{
			this.UpdateInputs();
			if (this._triggerPull.IsPulling && (this.TriggerPullProgress >= 1f || this.Cocked))
			{
				this.Fire(null);
			}
		}
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsCocked, this.Cocked);
		base.Firearm.AnimSetFloat(FirearmAnimatorHashes.TriggerState, this.TriggerPullProgress);
	}

	private void UpdateInputs()
	{
		if (base.Firearm.AnyModuleBusy() || base.PrimaryActionBlocked)
		{
			return;
		}
		if (base.GetAction(ActionName.WeaponAlt))
		{
			this._cockingKeyHoldTime += Time.deltaTime;
		}
		else if (this._cockingKeyHoldTime > 0f)
		{
			if (this._cockingKeyHoldTime < 0.5f)
			{
				this._cockRequestTimer.Trigger();
				this.SendCmd(MessageType.StartCocking);
			}
			this._cockingKeyHoldTime = 0f;
		}
		if (base.Firearm.TryGetModule<ITriggerControllerModule>(out var module) && module.TriggerHeld)
		{
			this._triggerPull.Pull(this.DoubleActionTime);
			if (!this.Cocked)
			{
				this.SendCmd(MessageType.StartPulling);
				this._audioModule.PlayClientside(this._doubleActionClip);
			}
		}
	}

	private void Fire(NetworkReader extraData)
	{
		if (base.IsLocalPlayer)
		{
			this._triggerPull.Reset();
			this._clientShotCooldown.Trigger(this.TimeBetweenShots);
		}
		else
		{
			if (!this._serverShotCooldown.Ready)
			{
				return;
			}
			this._serverPullTokenUsed = false;
			this._serverShotCooldown.Trigger(this.TimeBetweenShots);
		}
		if (!this.Cocked)
		{
			this._cylinderModule.RotateCylinder(1);
		}
		else
		{
			this.Cocked = false;
		}
		CylinderAmmoModule.Chamber chamber = this._cylinderModule.Chambers[0];
		if (chamber.ContextState == CylinderAmmoModule.ChamberState.Live)
		{
			this.FireLive(chamber, extraData);
		}
		else
		{
			this.FireDry();
		}
		if (base.IsLocalPlayer && !base.IsServer)
		{
			this.SendCmd(MessageType.CmdShoot, delegate(NetworkWriter x)
			{
				x.WriteBacktrackData(new ShotBacktrackData(base.Firearm));
			});
		}
	}

	private void FireLive(CylinderAmmoModule.Chamber chamber, NetworkReader extraData)
	{
		BulletShotEvent shotEvent = new BulletShotEvent(new ItemIdentifier(base.Firearm));
		if (base.IsServer)
		{
			PlayerShootingWeaponEventArgs e = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnShootingWeapon(e);
			if (!e.IsAllowed)
			{
				return;
			}
			(base.IsLocalPlayer ? new ShotBacktrackData(base.Firearm) : new ShotBacktrackData(extraData)).ProcessShot(base.Firearm, delegate(ReferenceHub target)
			{
				this._hitregModule.Fire(target, shotEvent);
			});
			this.SendRpc((ReferenceHub hub) => hub != base.Firearm.Owner && !hub.isLocalPlayer, delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageType.RpcFire);
			});
			PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
		}
		float num = base.Firearm.AttachmentsValue(AttachmentParam.ShotClipIdOverride);
		this._audioModule.PlayGunshot(this._fireClips[(int)num]);
		chamber.ContextState = CylinderAmmoModule.ChamberState.Discharged;
		ShotEventManager.Trigger(shotEvent);
		this.PlayFireAnims(FirearmAnimatorHashes.Fire);
	}

	private void FireDry()
	{
		if (base.IsServer)
		{
			PlayerDryFiringWeaponEventArgs e = new PlayerDryFiringWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnDryFiringWeapon(e);
			if (!e.IsAllowed)
			{
				return;
			}
			this.SendRpc(MessageType.RpcDryFire);
			PlayerEvents.OnDryFiredWeapon(new PlayerDryFiredWeaponEventArgs(base.Firearm.Owner, base.Firearm));
		}
		if (base.IsLocalPlayer)
		{
			this.PlayFireAnims(FirearmAnimatorHashes.DryFire);
		}
		this._audioModule.PlayNormal(this._dryFireClip);
	}

	private void PlayFireAnims(int hash)
	{
		base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, UnityEngine.Random.value, checkIfExists: true);
		base.Firearm.AnimSetTrigger(hash);
	}

	private MessageType DecodeHeader(NetworkReader reader)
	{
		return (MessageType)reader.ReadByte();
	}

	private void SendRpc(MessageType header, Action<NetworkWriter> writerFunc = null, bool toAll = true)
	{
		this.SendRpc(delegate(NetworkWriter x)
		{
			x.WriteSubheader(header);
			writerFunc?.Invoke(x);
		}, toAll);
	}

	private void SendCmd(MessageType header, Action<NetworkWriter> writerFunc = null)
	{
		this.SendCmd(delegate(NetworkWriter x)
		{
			x.WriteSubheader(header);
			writerFunc?.Invoke(x);
		});
	}
}
