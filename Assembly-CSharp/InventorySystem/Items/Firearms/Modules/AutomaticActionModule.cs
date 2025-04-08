using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AutomaticActionModule : ModuleBase, IActionModule, IAmmoContainerModule, IBusyIndicatorModule, IDisplayableAmmoProviderModule, IInspectPreventerModule
	{
		public float BaseFireRate { get; private set; }

		public float BoltTravelTime { get; private set; }

		public bool MagLocksBolt { get; private set; }

		public bool OpenBolt { get; private set; }

		public int ChamberSize { get; private set; }

		public static event Action<ushort> OnSyncDataReceived;

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
				return !this._serverQueuedRequests.Idle || !this._clientRateLimiter.Ready;
			}
		}

		public float DisplayCyclicRate
		{
			get
			{
				return this.BaseFireRate * base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);
			}
		}

		public int AmmoMax
		{
			get
			{
				return this.AmmoStored;
			}
		}

		public DisplayAmmoValues PredictedDisplayAmmo
		{
			get
			{
				return new DisplayAmmoValues(this._clientAmmo.Value, this._clientChambered.Value);
			}
		}

		public bool InspectionAllowed
		{
			get
			{
				return !this.IsBusy || (this._clientChambered.Value == 0 && this._clientAmmo.Value == 0);
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

		private float TimeBetweenShots
		{
			get
			{
				return 1f / this.DisplayCyclicRate;
			}
		}

		private float CurTime
		{
			get
			{
				return (float)this._totalTimeStopwatch.Elapsed.TotalSeconds;
			}
		}

		private AutomaticActionModule.SyncDataFlags CurSyncFlags
		{
			get
			{
				AutomaticActionModule.SyncDataFlags syncDataFlags;
				if (!AutomaticActionModule.ReceivedFlags.TryGetValue(base.ItemSerial, out syncDataFlags))
				{
					return AutomaticActionModule.SyncDataFlags.None;
				}
				return syncDataFlags;
			}
		}

		private int SyncAmmoChambered
		{
			get
			{
				return (int)(this.CurSyncFlags & AutomaticActionModule.SyncDataFlags.AmmoChamberedFilter);
			}
		}

		private bool SyncBoltLocked
		{
			get
			{
				return (this.CurSyncFlags & AutomaticActionModule.SyncDataFlags.BoltLocked) > AutomaticActionModule.SyncDataFlags.None;
			}
		}

		private bool SyncCocked
		{
			get
			{
				return (this.CurSyncFlags & AutomaticActionModule.SyncDataFlags.Cocked) > AutomaticActionModule.SyncDataFlags.None;
			}
		}

		private bool MagInserted
		{
			get
			{
				IMagazineControllerModule magazineControllerModule;
				return !base.Firearm.TryGetModule(out magazineControllerModule, true) || magazineControllerModule.MagazineInserted;
			}
		}

		private IPrimaryAmmoContainerModule PrimaryAmmoContainer
		{
			get
			{
				IPrimaryAmmoContainerModule primaryAmmoContainerModule;
				if (!base.Firearm.TryGetModule(out primaryAmmoContainerModule, true))
				{
					throw new InvalidOperationException("Automatic weapons must have a designated primary ammo container.");
				}
				return primaryAmmoContainerModule;
			}
		}

		public static void DecodeSyncFlags(ushort serial, out int ammoChambered, out bool boltLocked, out bool cocked)
		{
			AutomaticActionModule.SyncDataFlags syncDataFlags;
			if (!AutomaticActionModule.ReceivedFlags.TryGetValue(serial, out syncDataFlags))
			{
				syncDataFlags = AutomaticActionModule.SyncDataFlags.None;
			}
			ammoChambered = (int)(syncDataFlags & AutomaticActionModule.SyncDataFlags.AmmoChamberedFilter);
			boltLocked = (syncDataFlags & AutomaticActionModule.SyncDataFlags.BoltLocked) > AutomaticActionModule.SyncDataFlags.None;
			cocked = (syncDataFlags & AutomaticActionModule.SyncDataFlags.Cocked) > AutomaticActionModule.SyncDataFlags.None;
		}

		[ExposedFirearmEvent]
		public void ServerCycleAction()
		{
			if (!base.IsServer)
			{
				return;
			}
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

		[ExposedFirearmEvent]
		public void ServerUnloadChambered()
		{
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			if (!base.IsServer || !base.Firearm.TryGetModule(out primaryAmmoContainerModule, true))
			{
				return;
			}
			base.Firearm.OwnerInventory.ServerAddAmmo(primaryAmmoContainerModule.AmmoType, this.AmmoStored);
			this.AmmoStored = 0;
			this.ServerResync();
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
				this._serverQueuedRequests.Enqueue(new AutomaticActionModule.ShotRequest(reader));
			}
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			switch (reader.ReadByte())
			{
			case 2:
				this._clientCocked.Value &= reader.ReadBool();
				this._clientAmmo.Value -= (int)reader.ReadShort();
				return;
			case 3:
				if (base.IsSpectator)
				{
					this.PlayFireAnims(false);
					return;
				}
				break;
			case 4:
				if (base.IsSpectator)
				{
					this.PlayFireAnims(true);
				}
				break;
			default:
				return;
			}
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			AutomaticActionModule.<>c__DisplayClass82_0 CS$<>8__locals1;
			CS$<>8__locals1.reader = reader;
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.serial = serial;
			base.ClientProcessRpcTemplate(CS$<>8__locals1.reader, CS$<>8__locals1.serial);
			switch (CS$<>8__locals1.reader.ReadByte())
			{
			case 1:
				this.<ClientProcessRpcTemplate>g__ReadSyncData|82_0(ref CS$<>8__locals1);
				return;
			case 2:
			case 4:
				break;
			case 3:
			{
				int num = (int)CS$<>8__locals1.reader.ReadByte();
				for (int i = 0; i < num; i++)
				{
					ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm.ItemTypeId, CS$<>8__locals1.serial), i));
				}
				break;
			}
			case 5:
				while (CS$<>8__locals1.reader.Remaining > 0)
				{
					CS$<>8__locals1.serial = CS$<>8__locals1.reader.ReadUShort();
					this.<ClientProcessRpcTemplate>g__ReadSyncData|82_0(ref CS$<>8__locals1);
				}
				return;
			default:
				return;
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
			AudioModule audioModule;
			if (!base.Firearm.TryGetModule(out audioModule, true))
			{
				return;
			}
			audioModule.RegisterClip(this._dryfireSound);
			AutomaticActionModule.GunshotDefinition[] gunshotSounds = this._gunshotSounds;
			for (int i = 0; i < gunshotSounds.Length; i++)
			{
				gunshotSounds[i].RandomSounds.ForEach(new Action<AudioClip>(audioModule.RegisterClip));
			}
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			if (!base.IsServer)
			{
				return;
			}
			FullAutoShotsQueue<AutomaticActionModule.ShotRequest> serverQueuedRequests = this._serverQueuedRequests;
			serverQueuedRequests.OnRequestRejected = (Action<AutomaticActionModule.ShotRequest>)Delegate.Combine(serverQueuedRequests.OnRequestRejected, new Action<AutomaticActionModule.ShotRequest>(this.ServerSendResponse));
			this.BoltLocked = this.SyncBoltLocked;
			this.Cocked = this.SyncCocked;
			this.AmmoStored = this.SyncAmmoChambered;
			this.ServerResync();
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			if (!base.IsServer)
			{
				return;
			}
			if (!this.MagInserted)
			{
				this.BoltLocked = false;
				this.ServerResync();
			}
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (base.IsLocalPlayer)
			{
				this.ProcessInput();
				this.ProcessClientShots();
			}
			if (base.IsServer)
			{
				this.UpdateServer();
			}
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsCocked, base.IsLocalPlayer ? this._clientCocked.Value : this.Cocked, true);
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsBoltLocked, base.IsLocalPlayer ? this._clientBoltLock.Value : this.BoltLocked, true);
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.ChamberedAmmo, base.IsLocalPlayer ? this._clientChambered.Value : this.AmmoStored, true);
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.PredictedActionAmmo, this._clientAmmo.Value, true);
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
				writer.WriteSubheader(AutomaticActionModule.MessageHeader.RpcNewPlayerSync);
				foreach (KeyValuePair<ushort, AutomaticActionModule.SyncDataFlags> keyValuePair in AutomaticActionModule.ReceivedFlags)
				{
					if (keyValuePair.Value != AutomaticActionModule.SyncDataFlags.None)
					{
						writer.WriteUShort(keyValuePair.Key);
						writer.WriteByte((byte)keyValuePair.Value);
					}
				}
			});
		}

		private void ProcessInput()
		{
			if (!base.Firearm.IsLocalPlayer)
			{
				return;
			}
			this._clientRateLimiter.Update();
			ITriggerControllerModule triggerControllerModule;
			if (!base.Firearm.TryGetModule(out triggerControllerModule, true))
			{
				return;
			}
			if (!triggerControllerModule.TriggerHeld || !this._clientCocked.Value || !this._clientRateLimiter.Ready)
			{
				return;
			}
			this._clientRateLimiter.Trigger(this.TimeBetweenShots);
			this._clientQueuedShots.Enqueue(this.CurTime + this.BoltTravelTime);
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
				this._clientChambered.Value = (this.OpenBolt ? 0 : num2);
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
			AutomaticActionModule.ShotRequest request = new AutomaticActionModule.ShotRequest(this);
			this.SendCmd(delegate(NetworkWriter writer)
			{
				request.Write(writer);
			});
		}

		private void UpdateServer()
		{
			this._serverQueuedRequests.Update();
			if (base.Firearm.AnyModuleBusy(this))
			{
				return;
			}
			AutomaticActionModule.ShotRequest shotRequest;
			if (!this._serverQueuedRequests.TryDequeue(out shotRequest))
			{
				return;
			}
			if (!this.Cocked || this.BoltLocked)
			{
				return;
			}
			if (this.AmmoStored > 0 || (this.OpenBolt && this.PrimaryAmmoContainer.AmmoStored > 0))
			{
				PlayerShootingWeaponEventArgs playerShootingWeaponEventArgs = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
				PlayerEvents.OnShootingWeapon(playerShootingWeaponEventArgs);
				if (!playerShootingWeaponEventArgs.IsAllowed)
				{
					return;
				}
				shotRequest.BacktrackData.ProcessShot(base.Firearm, new Action<ReferenceHub>(this.ServerShoot));
				PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
			else
			{
				PlayerDryFiringWeaponEventArgs playerDryFiringWeaponEventArgs = new PlayerDryFiringWeaponEventArgs(base.Firearm.Owner, base.Firearm);
				PlayerEvents.OnDryFiringWeapon(playerDryFiringWeaponEventArgs);
				if (!playerDryFiringWeaponEventArgs.IsAllowed)
				{
					return;
				}
				this.Cocked = false;
				this.PlayDryFire();
				this.ServerResync();
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(AutomaticActionModule.MessageHeader.RpcDryFire);
				}, true);
				PlayerEvents.OnDryFiredWeapon(new PlayerDryFiredWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
			this.ServerSendResponse(shotRequest);
		}

		private void ServerSendResponse(AutomaticActionModule.ShotRequest request)
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(AutomaticActionModule.MessageHeader.RpcResponse);
				int num = request.PredictedReserve - this.PrimaryAmmoContainer.AmmoStored;
				writer.WriteBool(this.Cocked);
				writer.WriteShort((short)num);
			}, false);
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
			this.SendRpc((ReferenceHub ply) => ply != this.Firearm.Owner, delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(AutomaticActionModule.MessageHeader.RpcFire);
				writer.WriteByte((byte)ammoToFire);
			});
			this.PlayFire(ammoToFire);
			this.ServerCycleAction();
			IHitregModule hitregModule;
			if (!base.Firearm.TryGetModule(out hitregModule, true))
			{
				return;
			}
			for (int i = 0; i < ammoToFire; i++)
			{
				hitregModule.Fire(primaryTarget, new BulletShotEvent(new ItemIdentifier(base.Firearm), i));
			}
		}

		private void ServerResync()
		{
			AutomaticActionModule.SyncDataFlags syncData = (AutomaticActionModule.SyncDataFlags)this.AmmoStored;
			if (this.Cocked)
			{
				syncData |= AutomaticActionModule.SyncDataFlags.Cocked;
			}
			if (this.BoltLocked)
			{
				syncData |= AutomaticActionModule.SyncDataFlags.BoltLocked;
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(AutomaticActionModule.MessageHeader.RpcPublicSync);
				writer.WriteByte((byte)syncData);
			}, true);
		}

		private void PlayDryFire()
		{
			this.PlayFireAnims(true);
			AudioModule audioModule;
			if (!base.Firearm.TryGetModule(out audioModule, true))
			{
				return;
			}
			audioModule.PlayNormal(this._dryfireSound);
		}

		private void PlayFire(int chambersFired)
		{
			this.PlayFireAnims(false);
			AudioModule audioModule;
			if (!base.Firearm.TryGetModule(out audioModule, true))
			{
				return;
			}
			int num = (int)base.Firearm.AttachmentsValue(AttachmentParam.ShotClipIdOverride);
			foreach (AutomaticActionModule.GunshotDefinition gunshotDefinition in this._gunshotSounds)
			{
				if (gunshotDefinition.ClipId == num && gunshotDefinition.MinChamberedRounds <= chambersFired && gunshotDefinition.MaxChamberedRounds >= chambersFired)
				{
					audioModule.PlayGunshot(gunshotDefinition.RandomSounds.RandomItem<AudioClip>());
					return;
				}
			}
		}

		private void PlayFireAnims(bool dryFire)
		{
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, global::UnityEngine.Random.value, true);
			if (dryFire)
			{
				base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.DryFire, true);
				return;
			}
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Fire, false);
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.ReleaseHammer, true);
		}

		public int GetAmmoStoredForSerial(ushort serial)
		{
			AutomaticActionModule.SyncDataFlags syncDataFlags;
			if (!AutomaticActionModule.ReceivedFlags.TryGetValue(serial, out syncDataFlags))
			{
				return 0;
			}
			return (int)(syncDataFlags & AutomaticActionModule.SyncDataFlags.AmmoChamberedFilter);
		}

		public DisplayAmmoValues GetDisplayAmmoForSerial(ushort serial)
		{
			return new DisplayAmmoValues(0, this.GetAmmoStoredForSerial(serial));
		}

		[CompilerGenerated]
		private void <ClientProcessRpcTemplate>g__ReadSyncData|82_0(ref AutomaticActionModule.<>c__DisplayClass82_0 A_1)
		{
			if (A_1.reader.Remaining == 0)
			{
				global::UnityEngine.Debug.LogError(string.Format("Failed to deserialize {0} of {1} with serial {2}.", "AutomaticActionModule", base.Firearm.ItemTypeId, A_1.serial));
				return;
			}
			AutomaticActionModule.ReceivedFlags[A_1.serial] = (AutomaticActionModule.SyncDataFlags)A_1.reader.ReadByte();
			Action<ushort> onSyncDataReceived = AutomaticActionModule.OnSyncDataReceived;
			if (onSyncDataReceived == null)
			{
				return;
			}
			onSyncDataReceived(A_1.serial);
		}

		private static readonly Dictionary<ushort, AutomaticActionModule.SyncDataFlags> ReceivedFlags = new Dictionary<ushort, AutomaticActionModule.SyncDataFlags>();

		private const int MaxChambers = 16;

		private readonly Stopwatch _totalTimeStopwatch = Stopwatch.StartNew();

		private readonly FullAutoShotsQueue<AutomaticActionModule.ShotRequest> _serverQueuedRequests = new FullAutoShotsQueue<AutomaticActionModule.ShotRequest>((AutomaticActionModule.ShotRequest x) => x.BacktrackData);

		private readonly Queue<float> _clientQueuedShots = new Queue<float>();

		private readonly FullAutoRateLimiter _clientRateLimiter = new FullAutoRateLimiter();

		private ClientPredictedValue<int> _clientChambered;

		private ClientPredictedValue<int> _clientAmmo;

		private ClientPredictedValue<bool> _clientCocked;

		private ClientPredictedValue<bool> _clientBoltLock;

		private int _serverChambered;

		private bool _serverCocked;

		private bool _serverBoltLocked;

		[SerializeField]
		private AutomaticActionModule.GunshotDefinition[] _gunshotSounds;

		[SerializeField]
		private AudioClip _dryfireSound;

		[SerializeField]
		private AnimatorConditionalOverride _boltLockOverrideLayers;

		private enum MessageHeader
		{
			CmdShoot,
			RpcPublicSync,
			RpcResponse,
			RpcFire,
			RpcDryFire,
			RpcNewPlayerSync
		}

		[Flags]
		private enum SyncDataFlags : byte
		{
			None = 0,
			AmmoChamberedBit0 = 1,
			AmmoChamberedBit1 = 2,
			AmmoChamberedBit2 = 4,
			AmmoChamberedBit3 = 8,
			Cocked = 16,
			BoltLocked = 32,
			AmmoChamberedFilter = 15
		}

		private readonly struct ShotRequest
		{
			public ShotRequest(AutomaticActionModule mod)
			{
				this.PredictedReserve = mod._clientAmmo.Value;
				this.BacktrackData = new ShotBacktrackData(mod.Firearm);
			}

			public ShotRequest(NetworkReader reader)
			{
				this.PredictedReserve = (int)reader.ReadByte();
				this.BacktrackData = new ShotBacktrackData(reader);
			}

			public void Write(NetworkWriter writer)
			{
				writer.WriteSubheader(AutomaticActionModule.MessageHeader.CmdShoot);
				writer.WriteByte((byte)this.PredictedReserve);
				writer.WriteBacktrackData(this.BacktrackData);
			}

			public readonly int PredictedReserve;

			public readonly ShotBacktrackData BacktrackData;
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
	}
}
