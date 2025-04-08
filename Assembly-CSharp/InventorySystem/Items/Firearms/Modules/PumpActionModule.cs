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

namespace InventorySystem.Items.Firearms.Modules
{
	public class PumpActionModule : ModuleBase, IActionModule, IAmmoContainerModule, IBusyIndicatorModule, ITriggerPressPreventerModule, IReloadUnloadValidatorModule, IDisplayableAmmoProviderModule, IInspectPreventerModule
	{
		private float PumpingDuration
		{
			get
			{
				return this._basePumpingDuration / base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);
			}
		}

		private bool PumpIdle
		{
			get
			{
				return this._pumpingRemaining.Ready && !this._pumpingScheduled;
			}
		}

		private float CooldownAfterShot
		{
			get
			{
				return this._baseCooldownAfterShot / base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);
			}
		}

		private int ShotsPerTriggerPull
		{
			get
			{
				return Mathf.RoundToInt((float)this._baseShotsPerTriggerPull * base.Firearm.AttachmentsValue(AttachmentParam.AmmoConsumptionMultiplier));
			}
		}

		private int MagazineAmmo
		{
			get
			{
				IPrimaryAmmoContainerModule primaryAmmoContainerModule;
				if (!base.Firearm.TryGetModule(out primaryAmmoContainerModule, true))
				{
					return 0;
				}
				return primaryAmmoContainerModule.AmmoStored;
			}
		}

		private int SyncChambered
		{
			get
			{
				return this.GetSyncValue(PumpActionModule.ChamberedBySerial);
			}
			set
			{
				this.SetSyncValue(PumpActionModule.ChamberedBySerial, value);
			}
		}

		private int SyncCocked
		{
			get
			{
				return this.GetSyncValue(PumpActionModule.CockedBySerial);
			}
			set
			{
				this.SetSyncValue(PumpActionModule.CockedBySerial, value);
			}
		}

		public float DisplayCyclicRate
		{
			get
			{
				int shotsPerTriggerPull = this.ShotsPerTriggerPull;
				float cooldownAfterShot = this.CooldownAfterShot;
				float num = (float)(this._numberOfBarrels / shotsPerTriggerPull - 1) * cooldownAfterShot;
				float num2 = (float)shotsPerTriggerPull * cooldownAfterShot;
				float num3 = this.PumpingDuration + num2 + num;
				return (float)this._numberOfBarrels / num3;
			}
		}

		public int AmmoStored
		{
			get
			{
				return this.SyncChambered;
			}
		}

		public int AmmoMax
		{
			get
			{
				return this.AmmoStored;
			}
		}

		public bool IsBusy
		{
			get
			{
				return !this._clientRateLimiter.Ready || !this._serverQueuedShots.Idle || !this.PumpIdle;
			}
		}

		public bool ClientBlockTrigger
		{
			get
			{
				return !this.PumpIdle && this._clientRateLimiter.Ready;
			}
		}

		public IReloadUnloadValidatorModule.Authorization ReloadAuthorization
		{
			get
			{
				return IReloadUnloadValidatorModule.Authorization.Idle;
			}
		}

		public IReloadUnloadValidatorModule.Authorization UnloadAuthorization
		{
			get
			{
				if (this.AmmoStored <= 0)
				{
					return IReloadUnloadValidatorModule.Authorization.Idle;
				}
				return IReloadUnloadValidatorModule.Authorization.Allowed;
			}
		}

		public DisplayAmmoValues PredictedDisplayAmmo
		{
			get
			{
				return new DisplayAmmoValues(this._clientMagazine.Value, this._clientChambered.Value);
			}
		}

		public bool InspectionAllowed
		{
			get
			{
				return !this.IsBusy || (this._clientChambered.Value == 0 && this._clientMagazine.Value == 0);
			}
		}

		public bool IsLoaded
		{
			get
			{
				return this.AmmoStored > 0;
			}
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			if (base.IsServer)
			{
				this.ServerResync();
			}
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this._pumpingScheduled = false;
			this._pumpingRemaining.Clear();
		}

		protected override void OnInit()
		{
			base.OnInit();
			this._clientChambered = new ClientPredictedValue<int>(() => this.SyncChambered);
			this._clientCocked = new ClientPredictedValue<int>(() => this.SyncCocked);
			this._clientMagazine = new ClientPredictedValue<int>(() => this.MagazineAmmo);
			AudioModule audioModule;
			if (!base.Firearm.TryGetModule(out audioModule, true))
			{
				return;
			}
			this._shotClipPerBarrelIndex.ForEach(new Action<AudioClip>(audioModule.RegisterClip));
			audioModule.RegisterClip(this._dryFireClip);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (base.IsLocalPlayer)
			{
				this.UpdateClient();
			}
			if (base.IsServer)
			{
				this.UpdateServer();
			}
			int num = (base.IsLocalPlayer ? this._clientChambered.Value : this.SyncChambered);
			int num2 = (base.IsLocalPlayer ? this._clientCocked.Value : this.SyncCocked);
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.ChamberedAmmo, num, false);
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.CockedHammers, num2, false);
			this._pumpingDelayTimer.Update();
			this._pumpingRemaining.Update();
			if (this._pumpingScheduled && this._pumpingDelayTimer.Ready)
			{
				base.Firearm.AnimSetTrigger(PumpActionModule.PumpTriggerHash, false);
				this._pumpingScheduled = false;
				this._pumpingRemaining.Trigger(this.PumpingDuration);
			}
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
				writer.WriteSubheader(PumpActionModule.RpcType.ResyncAll);
				foreach (KeyValuePair<ushort, byte> keyValuePair in PumpActionModule.CockedBySerial)
				{
					writer.WriteUShort(keyValuePair.Key);
					writer.WriteByte((byte)PumpActionModule.GetChamberedCount(keyValuePair.Key));
					writer.WriteByte(keyValuePair.Value);
				}
			});
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			PumpActionModule.ChamberedBySerial.Clear();
			PumpActionModule.CockedBySerial.Clear();
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			this._serverQueuedShots.Enqueue(reader.ReadBacktrackData());
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
				PumpActionModule.ChamberedBySerial[serial] = reader.ReadByte();
				PumpActionModule.CockedBySerial[serial] = reader.ReadByte();
				return;
			case 1:
				while (reader.Remaining > 0)
				{
					serial = reader.ReadUShort();
					PumpActionModule.ChamberedBySerial[serial] = reader.ReadByte();
					PumpActionModule.CockedBySerial[serial] = reader.ReadByte();
				}
				return;
			case 2:
			{
				ItemIdentifier itemIdentifier = new ItemIdentifier(base.Firearm.ItemTypeId, serial);
				while (reader.Remaining > 0)
				{
					int num = (int)reader.ReadByte();
					ShotEventManager.Trigger(new BulletShotEvent(itemIdentifier, num));
				}
				return;
			}
			default:
				return;
			}
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			PumpActionModule.RpcType rpcType = (PumpActionModule.RpcType)reader.ReadByte();
			if (rpcType != PumpActionModule.RpcType.Shoot)
			{
				if (rpcType == PumpActionModule.RpcType.SchedulePump && base.Firearm.IsSpectator && !NetworkServer.active)
				{
					this.SchedulePumping((int)reader.ReadByte());
					return;
				}
			}
			else if (base.Firearm.IsSpectator)
			{
				base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Fire, false);
			}
		}

		public void Pump()
		{
			if (base.IsSpectator)
			{
				return;
			}
			if (!NetworkServer.active)
			{
				IReloaderModule reloaderModule;
				if (base.Firearm.TryGetModule(out reloaderModule, true) && reloaderModule.IsReloadingOrUnloading)
				{
					return;
				}
				this._clientCocked.Value = this._numberOfBarrels;
				this._clientChambered.Value = Mathf.Min(this._clientMagazine.Value, this._numberOfBarrels);
				this._clientMagazine.Value -= this._clientChambered.Value;
				return;
			}
			else
			{
				IPrimaryAmmoContainerModule primaryAmmoContainerModule;
				if (!base.Firearm.TryGetModule(out primaryAmmoContainerModule, true))
				{
					return;
				}
				if (this.AmmoStored > 0)
				{
					base.Firearm.OwnerInventory.ServerAddAmmo(primaryAmmoContainerModule.AmmoType, this.AmmoStored);
				}
				this.SyncCocked = this._numberOfBarrels;
				this.SyncChambered = Mathf.Min(primaryAmmoContainerModule.AmmoStored, this._numberOfBarrels);
				this.ServerResync();
				primaryAmmoContainerModule.ServerModifyAmmo(-this.SyncChambered);
				return;
			}
		}

		public DisplayAmmoValues GetDisplayAmmoForSerial(ushort serial)
		{
			return new DisplayAmmoValues(0, this.GetAmmoStoredForSerial(serial));
		}

		public int GetAmmoStoredForSerial(ushort serial)
		{
			return PumpActionModule.GetSyncValue(serial, PumpActionModule.ChamberedBySerial);
		}

		private void SchedulePumping(int shotsFired)
		{
			this._pumpingScheduled = true;
			this._pumpingDelayTimer.Trigger((float)shotsFired * this.CooldownAfterShot);
			if (!base.IsServer)
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(PumpActionModule.RpcType.SchedulePump);
				writer.WriteByte((byte)shotsFired);
			}, true);
		}

		private void UpdateClient()
		{
			this._clientRateLimiter.Update();
			if (!this._clientRateLimiter.Ready || !this.PumpIdle)
			{
				return;
			}
			ITriggerControllerModule triggerControllerModule;
			if (!base.Firearm.TryGetModule(out triggerControllerModule, true))
			{
				return;
			}
			if (!triggerControllerModule.TriggerHeld || this._clientCocked.Value == 0)
			{
				return;
			}
			ShotBacktrackData backtrackData = new ShotBacktrackData(base.Firearm);
			int num;
			this.PullTrigger(true, out num);
			this.SendCmd(delegate(NetworkWriter writer)
			{
				writer.WriteBacktrackData(backtrackData);
			});
			this._clientRateLimiter.Trigger(this.CooldownAfterShot);
		}

		private void PullTrigger(bool clientside, out int shotsFired)
		{
			int num = (clientside ? this._clientCocked.Value : this.SyncCocked);
			int num2 = Mathf.Min(this.ShotsPerTriggerPull, num);
			shotsFired = 0;
			for (int i = 0; i < num2; i++)
			{
				if (this.ShootOneBarrel(clientside))
				{
					shotsFired++;
				}
			}
			if (clientside && NetworkServer.active)
			{
				return;
			}
			if (num - num2 > 0)
			{
				return;
			}
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			if (!base.Firearm.TryGetModule(out primaryAmmoContainerModule, true))
			{
				return;
			}
			if (primaryAmmoContainerModule.AmmoStored == 0)
			{
				return;
			}
			this.SchedulePumping(shotsFired);
		}

		private bool ShootOneBarrel(bool clientside)
		{
			if (clientside)
			{
				ClientPredictedValue<int> clientCocked = this._clientCocked;
				int num = clientCocked.Value;
				clientCocked.Value = num - 1;
			}
			else
			{
				int num = this.SyncCocked;
				this.SyncCocked = num - 1;
			}
			int num2 = (clientside ? this._clientChambered.Value : this.SyncChambered);
			bool flag;
			if (num2 == 0)
			{
				this.PlaySound(delegate(AudioModule x)
				{
					x.PlayNormal(this._dryFireClip);
				}, clientside);
				flag = false;
			}
			else
			{
				num2--;
				int clipId = num2 % this._shotClipPerBarrelIndex.Length;
				this.PlaySound(delegate(AudioModule x)
				{
					x.PlayGunshot(this._shotClipPerBarrelIndex[clipId]);
				}, clientside);
				if (clientside)
				{
					base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Fire, false);
					ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm), num2));
				}
				flag = true;
			}
			if (clientside)
			{
				this._clientChambered.Value = num2;
			}
			else
			{
				this.SyncChambered = num2;
			}
			return flag;
		}

		private void PlaySound(Action<AudioModule> method, bool clientside)
		{
			if (clientside && NetworkServer.active)
			{
				return;
			}
			AudioModule audioModule;
			if (!base.Firearm.TryGetModule(out audioModule, true))
			{
				return;
			}
			method(audioModule);
		}

		private void UpdateServer()
		{
			this._serverQueuedShots.Update();
			if (base.Firearm.AnyModuleBusy(this))
			{
				return;
			}
			if (!this.PumpIdle)
			{
				return;
			}
			ShotBacktrackData shotBacktrackData;
			if (!this._serverQueuedShots.TryDequeue(out shotBacktrackData))
			{
				return;
			}
			PlayerShootingWeaponEventArgs playerShootingWeaponEventArgs = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnShootingWeapon(playerShootingWeaponEventArgs);
			if (!playerShootingWeaponEventArgs.IsAllowed)
			{
				return;
			}
			shotBacktrackData.ProcessShot(base.Firearm, new Action<ReferenceHub>(this.ServerProcessShot));
			PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
		}

		private void ServerProcessShot(ReferenceHub primaryTarget)
		{
			PumpActionModule.<>c__DisplayClass71_0 CS$<>8__locals1 = new PumpActionModule.<>c__DisplayClass71_0();
			CS$<>8__locals1.<>4__this = this;
			this.PullTrigger(false, out CS$<>8__locals1.shotsFired);
			this.ServerResync();
			if (CS$<>8__locals1.shotsFired == 0)
			{
				return;
			}
			this._serverQueuedShots.Trigger(this.CooldownAfterShot);
			CS$<>8__locals1.chambered = this.SyncChambered;
			this.SendRpc((ReferenceHub ply) => ply != CS$<>8__locals1.<>4__this.Firearm.Owner, new Action<NetworkWriter>(CS$<>8__locals1.<ServerProcessShot>g__WriteShots|0));
			IHitregModule hitregModule;
			if (!base.Firearm.TryGetModule(out hitregModule, true))
			{
				return;
			}
			ItemIdentifier itemIdentifier = new ItemIdentifier(base.Firearm);
			for (int i = 0; i < CS$<>8__locals1.shotsFired; i++)
			{
				int num = CS$<>8__locals1.chambered + i - 1;
				hitregModule.Fire(primaryTarget, new BulletShotEvent(itemIdentifier, num));
			}
		}

		private void ServerResync()
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(PumpActionModule.RpcType.ResyncOne);
				writer.WriteByte((byte)this.SyncChambered);
				writer.WriteByte((byte)this.SyncCocked);
			}, true);
		}

		private void SetSyncValue(Dictionary<ushort, byte> syncDictionary, int value)
		{
			syncDictionary[base.ItemSerial] = (byte)Mathf.Clamp(value, 0, 255);
		}

		private int GetSyncValue(Dictionary<ushort, byte> syncDictionary)
		{
			return PumpActionModule.GetSyncValue(base.ItemSerial, syncDictionary);
		}

		private static int GetSyncValue(ushort serial, Dictionary<ushort, byte> syncDictionary)
		{
			byte b;
			if (!syncDictionary.TryGetValue(serial, out b))
			{
				return 0;
			}
			return (int)b;
		}

		public static int GetChamberedCount(ushort serial)
		{
			return PumpActionModule.GetSyncValue(serial, PumpActionModule.ChamberedBySerial);
		}

		public static int GetCockedCount(ushort serial)
		{
			return PumpActionModule.GetSyncValue(serial, PumpActionModule.CockedBySerial);
		}

		private static readonly Dictionary<ushort, byte> ChamberedBySerial = new Dictionary<ushort, byte>();

		private static readonly Dictionary<ushort, byte> CockedBySerial = new Dictionary<ushort, byte>();

		private static readonly int PumpTriggerHash = Animator.StringToHash("Pump");

		private readonly FullAutoShotsQueue<ShotBacktrackData> _serverQueuedShots = new FullAutoShotsQueue<ShotBacktrackData>((ShotBacktrackData x) => x);

		private readonly FullAutoRateLimiter _clientRateLimiter = new FullAutoRateLimiter();

		private readonly FullAutoRateLimiter _pumpingDelayTimer = new FullAutoRateLimiter();

		private readonly FullAutoRateLimiter _pumpingRemaining = new FullAutoRateLimiter();

		private ClientPredictedValue<int> _clientChambered;

		private ClientPredictedValue<int> _clientCocked;

		private ClientPredictedValue<int> _clientMagazine;

		private bool _pumpingScheduled;

		[SerializeField]
		private int _numberOfBarrels;

		[SerializeField]
		private int _baseShotsPerTriggerPull;

		[SerializeField]
		private float _basePumpingDuration;

		[SerializeField]
		private float _baseCooldownAfterShot;

		[SerializeField]
		private AudioClip _dryFireClip;

		[SerializeField]
		private AudioClip[] _shotClipPerBarrelIndex;

		private enum RpcType
		{
			ResyncOne,
			ResyncAll,
			Shoot,
			SchedulePump
		}
	}
}
