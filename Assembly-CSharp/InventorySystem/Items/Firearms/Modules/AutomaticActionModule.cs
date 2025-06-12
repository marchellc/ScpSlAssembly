using System;
using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class AutomaticActionModule : ModuleBase, IActionModule, IAmmoContainerModule, IBusyIndicatorModule, IDisplayableAmmoProviderModule, IInspectPreventerModule
{
	private enum MessageHeader
	{
		CmdShoot,
		RpcPublicSync,
		RpcResponse,
		RpcFire,
		RpcDryFire,
		RpcNewPlayerSync,
		RpcRejectionReason
	}

	[Flags]
	private enum SyncDataFlags : byte
	{
		None = 0,
		AmmoChamberedBit0 = 1,
		AmmoChamberedBit1 = 2,
		AmmoChamberedBit2 = 4,
		AmmoChamberedBit3 = 8,
		Cocked = 0x10,
		BoltLocked = 0x20,
		AmmoChamberedFilter = 0xF
	}

	private readonly struct ShotRequest
	{
		public readonly int PredictedReserve;

		public readonly byte CorrectionVersion;

		public readonly ShotBacktrackData BacktrackData;

		public ShotRequest(AutomaticActionModule mod)
		{
			this.PredictedReserve = mod._clientAmmo.Value;
			this.CorrectionVersion = mod._clientCorrectionVersion;
			this.BacktrackData = new ShotBacktrackData(mod.Firearm);
		}

		public ShotRequest(NetworkReader reader)
		{
			this.PredictedReserve = reader.ReadByte();
			this.CorrectionVersion = reader.ReadByte();
			this.BacktrackData = new ShotBacktrackData(reader);
		}

		public void Write(NetworkWriter writer)
		{
			writer.WriteSubheader(MessageHeader.CmdShoot);
			writer.WriteByte((byte)this.PredictedReserve);
			writer.WriteByte(this.CorrectionVersion);
			writer.WriteBacktrackData(this.BacktrackData);
		}
	}

	[Serializable]
	private struct GunshotDefinition
	{
		public AudioClip[] RandomSounds;

		[Tooltip("Value provided in ShotClipIdOverride. Zero if no attachments.")]
		public int ClipId;

		[Tooltip("This gunshot sound will only be used if number of chambers fired is greater or equal to this amount.")]
		[Range(1f, 16f)]
		public int MinChamberedRounds;

		[Tooltip("This gunshot sound will only be used if number of chambers fired is less or equal to this amount.")]
		[Range(1f, 16f)]
		public int MaxChamberedRounds;
	}

	private enum RejectionReason
	{
		TimedOut = 1,
		ModuleBusy,
		NotCocked,
		BoltLocked
	}

	private static readonly Dictionary<ushort, SyncDataFlags> ReceivedFlags = new Dictionary<ushort, SyncDataFlags>();

	private const int MaxChambers = 16;

	private readonly Stopwatch _totalTimeStopwatch = Stopwatch.StartNew();

	private readonly FullAutoShotsQueue<ShotRequest> _serverQueuedRequests = new FullAutoShotsQueue<ShotRequest>((ShotRequest x) => x.BacktrackData);

	private readonly Queue<float> _clientQueuedShots = new Queue<float>();

	private readonly FullAutoRateLimiter _clientRateLimiter = new FullAutoRateLimiter();

	private ClientPredictedValue<int> _clientChambered;

	private ClientPredictedValue<int> _clientAmmo;

	private ClientPredictedValue<bool> _clientCocked;

	private ClientPredictedValue<bool> _clientBoltLock;

	private byte _clientCorrectionVersion;

	private int _serverChambered;

	private bool _serverCocked;

	private bool _serverBoltLocked;

	[SerializeField]
	private GunshotDefinition[] _gunshotSounds;

	[SerializeField]
	private AudioClip _dryfireSound;

	[SerializeField]
	private AnimatorConditionalOverride _boltLockOverrideLayers;

	[field: SerializeField]
	public virtual float BaseFireRate { get; private set; }

	[field: SerializeField]
	public float BoltTravelTime { get; private set; }

	[field: SerializeField]
	public bool MagLocksBolt { get; private set; }

	[field: SerializeField]
	public bool OpenBolt { get; private set; }

	[field: SerializeField]
	[field: Range(1f, 16f)]
	public int ChamberSize { get; private set; }

	public bool Cocked
	{
		get
		{
			if (!base.IsServer)
			{
				return this.SyncCocked;
			}
			return this._serverCocked;
		}
		private set
		{
			this._serverCocked = value;
		}
	}

	public bool BoltLocked
	{
		get
		{
			if (!base.IsServer)
			{
				return this.SyncBoltLocked;
			}
			return this._serverBoltLocked;
		}
		private set
		{
			this._serverBoltLocked = value;
		}
	}

	public int AmmoStored
	{
		get
		{
			if (!base.IsServer)
			{
				return this.SyncAmmoChambered;
			}
			return this._serverChambered;
		}
		private set
		{
			this._serverChambered = value;
		}
	}

	public bool IsBusy
	{
		get
		{
			if (this._serverQueuedRequests.Idle)
			{
				return !this._clientRateLimiter.Ready;
			}
			return true;
		}
	}

	public float DisplayCyclicRate => this.BaseFireRate * base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);

	public int AmmoMax => this.AmmoStored;

	public DisplayAmmoValues PredictedDisplayAmmo => new DisplayAmmoValues(this._clientAmmo.Value, this._clientChambered.Value);

	public bool InspectionAllowed
	{
		get
		{
			if (this.IsBusy)
			{
				if (this._clientChambered.Value == 0)
				{
					return this._clientAmmo.Value == 0;
				}
				return false;
			}
			return true;
		}
	}

	public bool IsLoaded
	{
		get
		{
			if (!this.OpenBolt)
			{
				return this.AmmoStored > 0;
			}
			return this.PrimaryAmmoContainer.AmmoStored > 0;
		}
	}

	private float TimeBetweenShots => 1f / this.DisplayCyclicRate;

	private float CurTime => (float)this._totalTimeStopwatch.Elapsed.TotalSeconds;

	private SyncDataFlags CurSyncFlags
	{
		get
		{
			if (!AutomaticActionModule.ReceivedFlags.TryGetValue(base.ItemSerial, out var value))
			{
				return SyncDataFlags.None;
			}
			return value;
		}
	}

	private int SyncAmmoChambered => (int)(this.CurSyncFlags & SyncDataFlags.AmmoChamberedFilter);

	private bool SyncBoltLocked => (this.CurSyncFlags & SyncDataFlags.BoltLocked) != 0;

	private bool SyncCocked => (this.CurSyncFlags & SyncDataFlags.Cocked) != 0;

	private bool MagInserted
	{
		get
		{
			if (base.Firearm.TryGetModule<IMagazineControllerModule>(out var module))
			{
				return module.MagazineInserted;
			}
			return true;
		}
	}

	private IPrimaryAmmoContainerModule PrimaryAmmoContainer
	{
		get
		{
			if (!base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
			{
				throw new InvalidOperationException("Automatic weapons must have a designated primary ammo container.");
			}
			return module;
		}
	}

	public static event Action<ushort> OnSyncDataReceived;

	public static void DecodeSyncFlags(ushort serial, out int ammoChambered, out bool boltLocked, out bool cocked)
	{
		if (!AutomaticActionModule.ReceivedFlags.TryGetValue(serial, out var value))
		{
			value = SyncDataFlags.None;
		}
		ammoChambered = (int)(value & SyncDataFlags.AmmoChamberedFilter);
		boltLocked = (value & SyncDataFlags.BoltLocked) != 0;
		cocked = (value & SyncDataFlags.Cocked) != 0;
	}

	[ExposedFirearmEvent]
	public void ServerCycleAction()
	{
		if (base.IsServer)
		{
			this.Cocked = true;
			if (!this.OpenBolt)
			{
				int num = Mathf.Min(this.PrimaryAmmoContainer.AmmoStored, this.ChamberSize - this.AmmoStored);
				this.AmmoStored += num;
				this.PrimaryAmmoContainer.ServerModifyAmmo(-num);
				this.BoltLocked = this.AmmoStored == 0 && this.MagLocksBolt && this.MagInserted;
			}
			this.ServerResync();
		}
	}

	[ExposedFirearmEvent]
	public void ServerUnloadChambered()
	{
		if (base.IsServer && base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
		{
			base.Firearm.OwnerInventory.ServerAddAmmo(module.AmmoType, this.AmmoStored);
			this.AmmoStored = 0;
			this.ServerResync();
		}
	}

	public void ServerLockBolt()
	{
		this.BoltLocked = true;
		this.ServerResync();
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (reader.ReadByte() == 0)
		{
			this._serverQueuedRequests.Enqueue(new ShotRequest(reader));
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		switch ((MessageHeader)reader.ReadByte())
		{
		case MessageHeader.RpcResponse:
			this.ClientApplyResponse(reader);
			break;
		case MessageHeader.RpcFire:
			if (base.IsSpectator)
			{
				this.PlayFireAnims(dryFire: false);
			}
			break;
		case MessageHeader.RpcDryFire:
			if (base.IsSpectator)
			{
				this.PlayFireAnims(dryFire: true);
			}
			break;
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		switch ((MessageHeader)reader.ReadByte())
		{
		case MessageHeader.RpcNewPlayerSync:
			while (reader.Remaining > 0)
			{
				serial = reader.ReadUShort();
				ReadSyncData();
			}
			break;
		case MessageHeader.RpcPublicSync:
			ReadSyncData();
			break;
		case MessageHeader.RpcFire:
		{
			int num = reader.ReadByte();
			for (int i = 0; i < num; i++)
			{
				ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm.ItemTypeId, serial), i));
			}
			break;
		}
		case MessageHeader.RpcRejectionReason:
		{
			byte b = reader.ReadByte();
			byte b2 = reader.ReadByte();
			UnityEngine.Debug.Log("Shot has been rejected by server. Error code: " + b + "." + b2);
			break;
		}
		case MessageHeader.RpcResponse:
		case MessageHeader.RpcDryFire:
			break;
		}
		void ReadSyncData()
		{
			if (reader.Remaining == 0)
			{
				UnityEngine.Debug.LogError(string.Format("Failed to deserialize {0} of {1} with serial {2}.", "AutomaticActionModule", base.Firearm.ItemTypeId, serial));
			}
			else
			{
				AutomaticActionModule.ReceivedFlags[serial] = (SyncDataFlags)reader.ReadByte();
				AutomaticActionModule.OnSyncDataReceived?.Invoke(serial);
			}
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		AutomaticActionModule.ReceivedFlags.Clear();
	}

	protected override void OnInit()
	{
		base.OnInit();
		this._clientChambered = new ClientPredictedValue<int>(() => this.SyncAmmoChambered);
		this._clientCocked = new ClientPredictedValue<bool>(() => this.SyncCocked);
		this._clientBoltLock = new ClientPredictedValue<bool>(() => this.SyncBoltLocked);
		this._clientAmmo = new ClientPredictedValue<int>(() => this.PrimaryAmmoContainer.AmmoStored);
		if (base.Firearm.TryGetModule<AudioModule>(out var module))
		{
			module.RegisterClip(this._dryfireSound);
			GunshotDefinition[] gunshotSounds = this._gunshotSounds;
			for (int num = 0; num < gunshotSounds.Length; num++)
			{
				gunshotSounds[num].RandomSounds.ForEach(module.RegisterClip);
			}
		}
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (base.IsServer)
		{
			FullAutoShotsQueue<ShotRequest> serverQueuedRequests = this._serverQueuedRequests;
			serverQueuedRequests.OnRequestTimedOut = (Action<ShotRequest>)Delegate.Combine(serverQueuedRequests.OnRequestTimedOut, new Action<ShotRequest>(ServerOnRequestTimedOut));
			this.BoltLocked = this.SyncBoltLocked;
			this.Cocked = this.SyncCocked;
			this.AmmoStored = this.SyncAmmoChambered;
			this.ServerResync();
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		if (base.IsServer && !this.MagInserted)
		{
			this.BoltLocked = false;
			this.ServerResync();
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsControllable)
		{
			this.ProcessInput();
			this.ProcessClientShots();
		}
		if (base.IsServer)
		{
			this.UpdateServer();
		}
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsCocked, base.IsLocalPlayer ? this._clientCocked.Value : this.Cocked, checkIfExists: true);
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsBoltLocked, base.IsLocalPlayer ? this._clientBoltLock.Value : this.BoltLocked, checkIfExists: true);
		base.Firearm.AnimSetInt(FirearmAnimatorHashes.ChamberedAmmo, base.IsLocalPlayer ? this._clientChambered.Value : this.AmmoStored, checkIfExists: true);
		base.Firearm.AnimSetInt(FirearmAnimatorHashes.PredictedActionAmmo, this._clientAmmo.Value, checkIfExists: true);
		this._boltLockOverrideLayers.Update(base.Firearm, this._clientBoltLock.Value || this.BoltLocked);
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
			writer.WriteSubheader(MessageHeader.RpcNewPlayerSync);
			foreach (KeyValuePair<ushort, SyncDataFlags> receivedFlag in AutomaticActionModule.ReceivedFlags)
			{
				if (receivedFlag.Value != SyncDataFlags.None)
				{
					writer.WriteUShort(receivedFlag.Key);
					writer.WriteByte((byte)receivedFlag.Value);
				}
			}
		});
	}

	protected virtual void OnTriggerHeld()
	{
		if (this._clientCocked.Value && this._clientRateLimiter.Ready)
		{
			this._clientRateLimiter.Trigger(this.TimeBetweenShots);
			this._clientQueuedShots.Enqueue(this.CurTime + this.BoltTravelTime);
		}
	}

	private void ProcessInput()
	{
		this._clientRateLimiter.Update();
		if (base.Firearm.TryGetModule<ITriggerControllerModule>(out var module) && module.TriggerHeld)
		{
			this.OnTriggerHeld();
		}
	}

	private void ProcessClientShots()
	{
		if (this._clientQueuedShots.Count == 0 || this._clientQueuedShots.Peek() > this.CurTime)
		{
			return;
		}
		this._clientQueuedShots.Dequeue();
		if (!this._clientCocked.Value || this._clientBoltLock.Value)
		{
			return;
		}
		int num = Mathf.Min((this.OpenBolt ? this._clientAmmo : this._clientChambered).Value, this.ChamberSize);
		if (num > 0)
		{
			for (int i = 0; i < num; i++)
			{
				ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm), i));
			}
			if (!NetworkServer.active)
			{
				this.PlayFire(num);
			}
			int num2 = Mathf.Min(this._clientAmmo.Value, this.ChamberSize);
			this._clientChambered.Value = ((!this.OpenBolt) ? num2 : 0);
			this._clientAmmo.Value -= num2;
			this._clientCocked.Value = true;
			if (num2 == 0 && this.MagLocksBolt && this.MagInserted)
			{
				this._clientBoltLock.Value = true;
			}
		}
		else
		{
			this.PlayDryFire();
			this._clientCocked.Value = false;
		}
		ShotRequest shotRequest = new ShotRequest(this);
		this.SendCmd(((ShotRequest)shotRequest).Write);
	}

	private void UpdateServer()
	{
		this._serverQueuedRequests.Update();
		if (!this._serverQueuedRequests.TryDequeue(out var dequeued))
		{
			return;
		}
		if (!this.Cocked)
		{
			this.ServerSendRejection(RejectionReason.NotCocked, 0);
			return;
		}
		if (this.BoltLocked)
		{
			this.ServerSendRejection(RejectionReason.BoltLocked, 0);
			return;
		}
		ModuleBase[] modules = base.Firearm.Modules;
		foreach (ModuleBase moduleBase in modules)
		{
			if (moduleBase is IBusyIndicatorModule { IsBusy: not false } && !(moduleBase == this))
			{
				this.ServerSendRejection(RejectionReason.ModuleBusy, moduleBase.SyncId);
				return;
			}
		}
		if (this.AmmoStored > 0 || (this.OpenBolt && this.PrimaryAmmoContainer.AmmoStored > 0))
		{
			PlayerShootingWeaponEventArgs e = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnShootingWeapon(e);
			if (!e.IsAllowed)
			{
				return;
			}
			dequeued.BacktrackData.ProcessShot(base.Firearm, ServerShoot);
			PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
		}
		else
		{
			PlayerDryFiringWeaponEventArgs e2 = new PlayerDryFiringWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnDryFiringWeapon(e2);
			if (!e2.IsAllowed)
			{
				return;
			}
			this.Cocked = false;
			this.PlayDryFire();
			this.ServerResync();
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(MessageHeader.RpcDryFire);
			});
			PlayerEvents.OnDryFiredWeapon(new PlayerDryFiredWeaponEventArgs(base.Firearm.Owner, base.Firearm));
		}
		this.ServerSendResponse(dequeued);
	}

	private void ClientApplyResponse(NetworkReader reader)
	{
		byte b = reader.ReadByte();
		bool flag = reader.ReadBool();
		int num = reader.ReadShort();
		this._clientCocked.Value &= flag;
		if (num != 0 && b == this._clientCorrectionVersion)
		{
			this._clientCorrectionVersion++;
			this._clientAmmo.Value -= num;
		}
	}

	private void ServerOnRequestTimedOut(ShotRequest request)
	{
		this.ServerSendResponse(request);
		this.ServerSendRejection(RejectionReason.TimedOut, 0);
	}

	private void ServerSendResponse(ShotRequest request)
	{
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MessageHeader.RpcResponse);
			int num = request.PredictedReserve - this.PrimaryAmmoContainer.AmmoStored;
			writer.WriteByte(request.CorrectionVersion);
			writer.WriteBool(this.Cocked);
			writer.WriteShort((short)num);
		}, toAll: false);
	}

	private void ServerSendRejection(RejectionReason reason, byte errorCode = 0)
	{
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MessageHeader.RpcRejectionReason);
			writer.WriteSubheader(reason);
			writer.WriteByte(errorCode);
		}, toAll: false);
	}

	private void ServerShoot(ReferenceHub primaryTarget)
	{
		int ammoToFire;
		if (this.OpenBolt)
		{
			ammoToFire = Mathf.Min(this.PrimaryAmmoContainer.AmmoStored, this.ChamberSize);
			this.PrimaryAmmoContainer.ServerModifyAmmo(-ammoToFire);
		}
		else
		{
			ammoToFire = this.AmmoStored;
			this.AmmoStored = 0;
		}
		this._serverQueuedRequests.Trigger(this.TimeBetweenShots);
		this.SendRpc((ReferenceHub ply) => ply != base.Firearm.Owner, delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MessageHeader.RpcFire);
			writer.WriteByte((byte)ammoToFire);
		});
		this.PlayFire(ammoToFire);
		this.ServerCycleAction();
		if (base.Firearm.TryGetModule<IHitregModule>(out var module))
		{
			for (int num = 0; num < ammoToFire; num++)
			{
				module.Fire(primaryTarget, new BulletShotEvent(new ItemIdentifier(base.Firearm), num));
			}
		}
	}

	private void ServerResync()
	{
		SyncDataFlags syncData = (SyncDataFlags)this.AmmoStored;
		if (this.Cocked)
		{
			syncData |= SyncDataFlags.Cocked;
		}
		if (this.BoltLocked)
		{
			syncData |= SyncDataFlags.BoltLocked;
		}
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(MessageHeader.RpcPublicSync);
			writer.WriteByte((byte)syncData);
		});
	}

	private void PlayDryFire()
	{
		this.PlayFireAnims(dryFire: true);
		if (base.Firearm.TryGetModule<AudioModule>(out var module))
		{
			module.PlayNormal(this._dryfireSound);
		}
	}

	private void PlayFire(int chambersFired)
	{
		this.PlayFireAnims(dryFire: false);
		if (!base.Firearm.TryGetModule<AudioModule>(out var module))
		{
			return;
		}
		int num = (int)base.Firearm.AttachmentsValue(AttachmentParam.ShotClipIdOverride);
		GunshotDefinition[] gunshotSounds = this._gunshotSounds;
		for (int i = 0; i < gunshotSounds.Length; i++)
		{
			GunshotDefinition gunshotDefinition = gunshotSounds[i];
			if (gunshotDefinition.ClipId == num && gunshotDefinition.MinChamberedRounds <= chambersFired && gunshotDefinition.MaxChamberedRounds >= chambersFired)
			{
				module.PlayGunshot(gunshotDefinition.RandomSounds.RandomItem());
				break;
			}
		}
	}

	private void PlayFireAnims(bool dryFire)
	{
		base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, UnityEngine.Random.value, checkIfExists: true);
		if (dryFire)
		{
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.DryFire, checkIfExists: true);
			return;
		}
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Fire);
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.ReleaseHammer, checkIfExists: true);
	}

	public int GetAmmoStoredForSerial(ushort serial)
	{
		if (!AutomaticActionModule.ReceivedFlags.TryGetValue(serial, out var value))
		{
			return 0;
		}
		return (int)(value & SyncDataFlags.AmmoChamberedFilter);
	}

	public DisplayAmmoValues GetDisplayAmmoForSerial(ushort serial)
	{
		return new DisplayAmmoValues(0, this.GetAmmoStoredForSerial(serial));
	}
}
