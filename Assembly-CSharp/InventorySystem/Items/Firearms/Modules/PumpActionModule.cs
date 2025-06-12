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

namespace InventorySystem.Items.Firearms.Modules;

public class PumpActionModule : ModuleBase, IActionModule, IAmmoContainerModule, IBusyIndicatorModule, ITriggerPressPreventerModule, IReloadUnloadValidatorModule, IDisplayableAmmoProviderModule, IInspectPreventerModule
{
	private enum RpcType
	{
		ResyncOne,
		ResyncAll,
		Shoot,
		SchedulePump
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

	private float PumpingDuration => this._basePumpingDuration / base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);

	private bool PumpIdle
	{
		get
		{
			if (this._pumpingRemaining.Ready)
			{
				return !this._pumpingScheduled;
			}
			return false;
		}
	}

	private float CooldownAfterShot => this._baseCooldownAfterShot / base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);

	private int ShotsPerTriggerPull => Mathf.RoundToInt((float)this._baseShotsPerTriggerPull * base.Firearm.AttachmentsValue(AttachmentParam.AmmoConsumptionMultiplier));

	private int MagazineAmmo
	{
		get
		{
			if (!base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
			{
				return 0;
			}
			return module.AmmoStored;
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

	public int AmmoStored => this.SyncChambered;

	public int AmmoMax => this.AmmoStored;

	public bool IsBusy
	{
		get
		{
			if (this._clientRateLimiter.Ready && this._serverQueuedShots.Idle)
			{
				return !this.PumpIdle;
			}
			return true;
		}
	}

	public bool ClientBlockTrigger
	{
		get
		{
			if (!this.PumpIdle)
			{
				return this._clientRateLimiter.Ready;
			}
			return false;
		}
	}

	public IReloadUnloadValidatorModule.Authorization ReloadAuthorization => IReloadUnloadValidatorModule.Authorization.Idle;

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

	public DisplayAmmoValues PredictedDisplayAmmo => new DisplayAmmoValues(this._clientMagazine.Value, this._clientChambered.Value);

	public bool InspectionAllowed
	{
		get
		{
			if (this.IsBusy)
			{
				if (this._clientChambered.Value == 0)
				{
					return this._clientMagazine.Value == 0;
				}
				return false;
			}
			return true;
		}
	}

	public bool IsLoaded => this.AmmoStored > 0;

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
		if (base.Firearm.TryGetModule<AudioModule>(out var module))
		{
			this._shotClipPerBarrelIndex.ForEach(module.RegisterClip);
			module.RegisterClip(this._dryFireClip);
		}
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
		int i = (base.IsLocalPlayer ? this._clientChambered.Value : this.SyncChambered);
		int i2 = (base.IsLocalPlayer ? this._clientCocked.Value : this.SyncCocked);
		base.Firearm.AnimSetInt(FirearmAnimatorHashes.ChamberedAmmo, i);
		base.Firearm.AnimSetInt(FirearmAnimatorHashes.CockedHammers, i2);
		this._pumpingDelayTimer.Update();
		this._pumpingRemaining.Update();
		if (this._pumpingScheduled && this._pumpingDelayTimer.Ready)
		{
			base.Firearm.AnimSetTrigger(PumpActionModule.PumpTriggerHash);
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
			writer.WriteSubheader(RpcType.ResyncAll);
			foreach (KeyValuePair<ushort, byte> item in PumpActionModule.CockedBySerial)
			{
				writer.WriteUShort(item.Key);
				writer.WriteByte((byte)PumpActionModule.GetChamberedCount(item.Key));
				writer.WriteByte(item.Value);
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
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.ResyncOne:
			PumpActionModule.ChamberedBySerial[serial] = reader.ReadByte();
			PumpActionModule.CockedBySerial[serial] = reader.ReadByte();
			break;
		case RpcType.ResyncAll:
			while (reader.Remaining > 0)
			{
				serial = reader.ReadUShort();
				PumpActionModule.ChamberedBySerial[serial] = reader.ReadByte();
				PumpActionModule.CockedBySerial[serial] = reader.ReadByte();
			}
			break;
		case RpcType.Shoot:
		{
			ItemIdentifier shotFirearm = new ItemIdentifier(base.Firearm.ItemTypeId, serial);
			while (reader.Remaining > 0)
			{
				int barrelId = reader.ReadByte();
				ShotEventManager.Trigger(new BulletShotEvent(shotFirearm, barrelId));
			}
			break;
		}
		}
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		switch ((RpcType)reader.ReadByte())
		{
		case RpcType.SchedulePump:
			if (base.Firearm.IsSpectator && !NetworkServer.active)
			{
				this.SchedulePumping(reader.ReadByte());
			}
			break;
		case RpcType.Shoot:
			if (base.Firearm.IsSpectator)
			{
				base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Fire);
			}
			break;
		}
	}

	public void Pump()
	{
		if (base.IsSpectator)
		{
			return;
		}
		IPrimaryAmmoContainerModule module2;
		if (!NetworkServer.active)
		{
			if (!base.Firearm.TryGetModule<IReloaderModule>(out var module) || !module.IsReloadingOrUnloading)
			{
				this._clientCocked.Value = this._numberOfBarrels;
				this._clientChambered.Value = Mathf.Min(this._clientMagazine.Value, this._numberOfBarrels);
				this._clientMagazine.Value -= this._clientChambered.Value;
			}
		}
		else if (base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out module2))
		{
			if (this.AmmoStored > 0)
			{
				base.Firearm.OwnerInventory.ServerAddAmmo(module2.AmmoType, this.AmmoStored);
			}
			this.SyncCocked = this._numberOfBarrels;
			this.SyncChambered = Mathf.Min(module2.AmmoStored, this._numberOfBarrels);
			this.ServerResync();
			module2.ServerModifyAmmo(-this.SyncChambered);
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
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(RpcType.SchedulePump);
				writer.WriteByte((byte)shotsFired);
			});
		}
	}

	private void UpdateClient()
	{
		this._clientRateLimiter.Update();
		if (this._clientRateLimiter.Ready && this.PumpIdle && base.Firearm.TryGetModule<ITriggerControllerModule>(out var module) && module.TriggerHeld && this._clientCocked.Value != 0)
		{
			ShotBacktrackData backtrackData = new ShotBacktrackData(base.Firearm);
			this.PullTrigger(clientside: true, out var _);
			this.SendCmd(delegate(NetworkWriter writer)
			{
				writer.WriteBacktrackData(backtrackData);
			});
			this._clientRateLimiter.Trigger(this.CooldownAfterShot);
		}
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
		if ((!clientside || !NetworkServer.active) && num - num2 <= 0 && base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module) && module.AmmoStored != 0)
		{
			this.SchedulePumping(shotsFired);
		}
	}

	private bool ShootOneBarrel(bool clientside)
	{
		if (clientside)
		{
			this._clientCocked.Value--;
		}
		else
		{
			this.SyncCocked--;
		}
		int num = (clientside ? this._clientChambered.Value : this.SyncChambered);
		bool result;
		if (num == 0)
		{
			this.PlaySound(delegate(AudioModule x)
			{
				x.PlayNormal(this._dryFireClip);
			}, clientside);
			result = false;
		}
		else
		{
			num--;
			int clipId = num % this._shotClipPerBarrelIndex.Length;
			this.PlaySound(delegate(AudioModule x)
			{
				x.PlayGunshot(this._shotClipPerBarrelIndex[clipId]);
			}, clientside);
			if (clientside)
			{
				base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Fire);
				ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm), num));
			}
			result = true;
		}
		if (clientside)
		{
			this._clientChambered.Value = num;
		}
		else
		{
			this.SyncChambered = num;
		}
		return result;
	}

	private void PlaySound(Action<AudioModule> method, bool clientside)
	{
		if ((!clientside || !NetworkServer.active) && base.Firearm.TryGetModule<AudioModule>(out var module))
		{
			method(module);
		}
	}

	private void UpdateServer()
	{
		this._serverQueuedShots.Update();
		if (!base.Firearm.AnyModuleBusy(this) && this.PumpIdle && this._serverQueuedShots.TryDequeue(out var dequeued))
		{
			PlayerShootingWeaponEventArgs e = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnShootingWeapon(e);
			if (e.IsAllowed)
			{
				dequeued.ProcessShot(base.Firearm, ServerProcessShot);
				PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
		}
	}

	private void ServerProcessShot(ReferenceHub primaryTarget)
	{
		this.PullTrigger(clientside: false, out var shotsFired);
		this.ServerResync();
		if (shotsFired == 0)
		{
			return;
		}
		this._serverQueuedShots.Trigger(this.CooldownAfterShot);
		int chambered = this.SyncChambered;
		this.SendRpc((ReferenceHub ply) => ply != base.Firearm.Owner, WriteShots);
		if (base.Firearm.TryGetModule<IHitregModule>(out var module))
		{
			ItemIdentifier shotFirearm = new ItemIdentifier(base.Firearm);
			for (int num = 0; num < shotsFired; num++)
			{
				int barrelId = chambered + num - 1;
				module.Fire(primaryTarget, new BulletShotEvent(shotFirearm, barrelId));
			}
		}
		void WriteShots(NetworkWriter writer)
		{
			writer.WriteSubheader(RpcType.Shoot);
			for (int num2 = shotsFired; num2 > 0; num2--)
			{
				int num3 = chambered + num2 - 1;
				writer.WriteByte((byte)num3);
			}
		}
	}

	private void ServerResync()
	{
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(RpcType.ResyncOne);
			writer.WriteByte((byte)this.SyncChambered);
			writer.WriteByte((byte)this.SyncCocked);
		});
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
		if (!syncDictionary.TryGetValue(serial, out var value))
		{
			return 0;
		}
		return value;
	}

	public static int GetChamberedCount(ushort serial)
	{
		return PumpActionModule.GetSyncValue(serial, PumpActionModule.ChamberedBySerial);
	}

	public static int GetCockedCount(ushort serial)
	{
		return PumpActionModule.GetSyncValue(serial, PumpActionModule.CockedBySerial);
	}
}
