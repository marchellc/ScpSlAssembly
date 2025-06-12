using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Achievements;
using Footprinting;
using GameCore;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Usables.Scp244;
using LabApi.Events.Arguments.WarheadEvents;
using LabApi.Events.Handlers;
using LightContainmentZoneDecontamination;
using MapGeneration;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using Subtitles;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

public class AlphaWarheadController : NetworkBehaviour
{
	[Serializable]
	public class DetonationScenario
	{
		public AudioClip Clip;

		public int TimeToDetonate;

		public int AdditionalTime;

		public int TotalTime => this.TimeToDetonate + this.AdditionalTime;
	}

	public DetonationScenario[] StartScenarios;

	public DetonationScenario[] ResumeScenarios;

	public DetonationScenario DeadmanSwitchScenario;

	public byte DefaultScenarioId;

	[SyncVar]
	public AlphaWarheadSyncInfo Info;

	[SyncVar]
	public double CooldownEndTime;

	internal static bool AutoWarheadBroadcastEnabled;

	internal static string WarheadBroadcastMessage;

	internal static string WarheadExplodedBroadcastMessage;

	internal static ushort WarheadBroadcastMessageTime;

	internal static ushort WarheadExplodedBroadcastMessageTime;

	internal static bool LockGatesOnCountdown;

	public const float InevitableTime = 10f;

	[SerializeField]
	private AudioClip _cancelSound;

	private AudioSource _alarmSource;

	private bool _doorsAlreadyOpen;

	private bool _blastDoorsShut;

	private bool _openDoors;

	private int _cooldown;

	private bool _isAutomatic;

	private bool _fogEnabled;

	private float _autoDetonateTime;

	private bool _autoDetonate;

	private bool _autoDetonateLock;

	private Footprint _triggeringPlayer;

	private AlphaWarheadSyncInfo _prevInfo;

	public bool AlreadyDetonated { get; private set; }

	public DetonationScenario CurScenario => this.Info.ScenarioType switch
	{
		WarheadScenarioType.Start => this.StartScenarios[this.Info.ScenarioId], 
		WarheadScenarioType.Resume => this.ResumeScenarios[this.Info.ScenarioId], 
		WarheadScenarioType.DeadmanSwitch => this.DeadmanSwitchScenario, 
		_ => null, 
	};

	public int WarheadKills { get; private set; }

	public bool IsLocked { get; set; }

	public static AlphaWarheadController Singleton { get; private set; }

	public static bool SingletonSet { get; private set; }

	public static ReferenceHub WarheadTriggeredby
	{
		get
		{
			if (!AlphaWarheadController.SingletonSet)
			{
				return null;
			}
			return AlphaWarheadController.Singleton._triggeringPlayer.Hub;
		}
	}

	public static bool Detonated
	{
		get
		{
			if (AlphaWarheadController.InProgress)
			{
				return AlphaWarheadController.TimeUntilDetonation == 0f;
			}
			return false;
		}
	}

	public static bool InProgress
	{
		get
		{
			if (AlphaWarheadController.SingletonSet)
			{
				return AlphaWarheadController.Singleton.Info.InProgress;
			}
			return false;
		}
	}

	public static float TimeUntilDetonation => Mathf.Max(0f, (float)(AlphaWarheadController.Singleton.Info.StartTime + (double)AlphaWarheadController.Singleton.CurScenario.TotalTime - NetworkTime.time));

	public AlphaWarheadSyncInfo NetworkInfo
	{
		get
		{
			return this.Info;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Info, 1uL, null);
		}
	}

	public double NetworkCooldownEndTime
	{
		get
		{
			return this.CooldownEndTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.CooldownEndTime, 2uL, null);
		}
	}

	public static event Action<bool> OnProgressChanged;

	public static event Action OnDetonated;

	private void Awake()
	{
		AlphaWarheadController.Singleton = this;
		AlphaWarheadController.SingletonSet = true;
	}

	private void Start()
	{
		this._alarmSource = base.GetComponent<AudioSource>();
		if (!NetworkServer.active)
		{
			return;
		}
		this.NetworkCooldownEndTime = 0.0;
		this._autoDetonateTime = ConfigFile.ServerConfig.GetFloat("auto_warhead_start_minutes") * 60f;
		this._autoDetonate = this._autoDetonateTime > 0f;
		this._autoDetonateLock = ConfigFile.ServerConfig.GetBool("auto_warhead_lock");
		this._openDoors = ConfigFile.ServerConfig.GetBool("open_doors_on_countdown", def: true);
		this._cooldown = ConfigFile.ServerConfig.GetInt("warhead_cooldown", 40);
		AlphaWarheadSyncInfo networkInfo = default(AlphaWarheadSyncInfo);
		int num = ConfigFile.ServerConfig.GetInt("warhead_tminus_start_duration", 90);
		networkInfo.ScenarioId = this.DefaultScenarioId;
		for (byte b = 0; b < this.StartScenarios.Length; b++)
		{
			if (this.StartScenarios[b].TimeToDetonate == num)
			{
				networkInfo.ScenarioId = b;
			}
		}
		this.NetworkInfo = networkInfo;
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(OnConfigReloaded));
	}

	private void OnDestroy()
	{
		AlphaWarheadController.SingletonSet = false;
		ConfigFile.OnConfigReloaded = (Action)Delegate.Remove(ConfigFile.OnConfigReloaded, new Action(OnConfigReloaded));
	}

	private void Update()
	{
		if (this.Info != this._prevInfo)
		{
			this.OnInfoUpdated();
			this._prevInfo = this.Info;
		}
		this.UpdateFog();
		this.ServerUpdateDetonationTime();
		this.ServerUpdateAutonuke();
	}

	private bool TryGetBroadcaster(out Broadcast broadcaster)
	{
		broadcaster = null;
		if (ReferenceHub.TryGetLocalHub(out var hub))
		{
			return hub.TryGetComponent<Broadcast>(out broadcaster);
		}
		return false;
	}

	private void OnInfoUpdated()
	{
		bool inProgress = this.Info.InProgress;
		if (inProgress != this._prevInfo.InProgress)
		{
			AlphaWarheadController.OnProgressChanged?.Invoke(inProgress);
		}
		this._alarmSource.Stop();
		if (!inProgress)
		{
			if (this._prevInfo.InProgress)
			{
				this._alarmSource.PlayOneShot(this._cancelSound);
			}
			return;
		}
		this._alarmSource.volume = 1f;
		this._alarmSource.clip = this.CurScenario.Clip;
		float num = (float)(NetworkTime.time - this.Info.StartTime);
		if (num < 0f)
		{
			this._alarmSource.PlayDelayed(0f - num);
		}
		else if (num < this._alarmSource.clip.length)
		{
			this._alarmSource.Play();
			this._alarmSource.time = num;
		}
	}

	public void ForceTime(float remaining)
	{
		this.InstantPrepare();
		this.StartDetonation(isAutomatic: false, suppressSubtitles: true);
		AlphaWarheadSyncInfo info = this.Info;
		remaining -= (float)this.CurScenario.TotalTime;
		info.StartTime = NetworkTime.time + (double)remaining;
		this.NetworkInfo = info;
	}

	public void InstantPrepare()
	{
		AlphaWarheadSyncInfo info = this.Info;
		info.StartTime = 0.0;
		this.NetworkInfo = info;
		this.NetworkCooldownEndTime = 0.0;
	}

	public void StartDetonation(bool isAutomatic = false, bool suppressSubtitles = false, ReferenceHub trigger = null)
	{
		if (this.Info.InProgress || this.CooldownEndTime > NetworkTime.time || this.IsLocked)
		{
			return;
		}
		AlphaWarheadSyncInfo info = this.Info;
		info.StartTime = NetworkTime.time;
		WarheadStartingEventArgs e = new WarheadStartingEventArgs((trigger == null) ? ReferenceHub.HostHub : trigger, isAutomatic, suppressSubtitles, info);
		WarheadEvents.OnStarting(e);
		if (!e.IsAllowed)
		{
			return;
		}
		isAutomatic = e.IsAutomatic;
		suppressSubtitles = e.SuppressSubtitles;
		info = e.WarheadState;
		trigger = e.Player.ReferenceHub;
		this._isAutomatic = isAutomatic;
		this.AlreadyDetonated = false;
		if (isAutomatic)
		{
			this.IsLocked |= this._autoDetonateLock;
			if (!this.AlreadyDetonated && !this.Info.InProgress && AlphaWarheadController.AutoWarheadBroadcastEnabled && this.TryGetBroadcaster(out var broadcaster))
			{
				broadcaster.RpcAddElement(AlphaWarheadController.WarheadBroadcastMessage, AlphaWarheadController.WarheadBroadcastMessageTime, Broadcast.BroadcastFlags.Normal);
			}
			this._autoDetonate = false;
		}
		this._doorsAlreadyOpen = false;
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Countdown started.", ServerLogs.ServerLogType.GameEvent);
		this._triggeringPlayer = new Footprint(trigger);
		this.NetworkInfo = info;
		WarheadEvents.OnStarted(new WarheadStartedEventArgs((trigger == null) ? ReferenceHub.HostHub : trigger, isAutomatic, suppressSubtitles, info));
		if (!suppressSubtitles)
		{
			SubtitleType subtitle = ((this.Info.ScenarioType == WarheadScenarioType.Resume) ? SubtitleType.AlphaWarheadResumed : SubtitleType.AlphaWarheadEngage);
			new SubtitleMessage(new SubtitlePart(subtitle, this.CurScenario.TimeToDetonate.ToString())).SendToAuthenticated();
		}
	}

	public void CancelDetonation()
	{
		this.CancelDetonation(null);
	}

	public void CancelDetonation(ReferenceHub disabler)
	{
		if (!this.Info.InProgress || AlphaWarheadController.TimeUntilDetonation <= 10f || this.IsLocked)
		{
			return;
		}
		AlphaWarheadSyncInfo info = this.Info;
		WarheadStoppingEventArgs e = new WarheadStoppingEventArgs((disabler == null) ? ReferenceHub.HostHub : disabler, info);
		WarheadEvents.OnStopping(e);
		if (!e.IsAllowed)
		{
			return;
		}
		info = e.WarheadState;
		disabler = e.Player.ReferenceHub;
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Detonation cancelled.", ServerLogs.ServerLogType.GameEvent);
		if (AlphaWarheadController.TimeUntilDetonation <= 15f && disabler != null)
		{
			AchievementHandlerBase.ServerAchieve(disabler.connectionToClient, AchievementName.ThatWasClose);
		}
		info.StartTime = 0.0;
		int num = (int)Mathf.Min(AlphaWarheadController.TimeUntilDetonation, this.CurScenario.TimeToDetonate);
		int num2 = int.MaxValue;
		info.ScenarioType = WarheadScenarioType.Resume;
		for (byte b = 0; b < this.ResumeScenarios.Length; b++)
		{
			int num3 = this.ResumeScenarios[b].TimeToDetonate - num;
			if (num3 >= 0 && num3 <= num2)
			{
				num2 = num3;
				info.ScenarioId = b;
			}
		}
		this.NetworkInfo = info;
		this.NetworkCooldownEndTime = NetworkTime.time + (double)this._cooldown;
		DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.WarheadCancel);
		if (NetworkServer.active)
		{
			this._isAutomatic = false;
			new SubtitleMessage(new SubtitlePart(SubtitleType.AlphaWarheadCancelled, (string[])null)).SendToAuthenticated();
			WarheadEvents.OnStopped(new WarheadStoppedEventArgs((disabler == null) ? ReferenceHub.HostHub : disabler, info));
		}
	}

	private void Detonate()
	{
		ReferenceHub player = ((this._triggeringPlayer.Hub == null) ? ReferenceHub.HostHub : this._triggeringPlayer.Hub);
		WarheadDetonatingEventArgs e = new WarheadDetonatingEventArgs(player);
		WarheadEvents.OnDetonating(e);
		if (!e.IsAllowed)
		{
			return;
		}
		this._triggeringPlayer = new Footprint(e.Player.ReferenceHub);
		AlphaWarheadController.OnDetonated?.Invoke();
		if (this._isAutomatic && !this.AlreadyDetonated && !this.Info.InProgress && AlphaWarheadController.AutoWarheadBroadcastEnabled && this.TryGetBroadcaster(out var broadcaster))
		{
			broadcaster.RpcAddElement(AlphaWarheadController.WarheadExplodedBroadcastMessage, AlphaWarheadController.WarheadExplodedBroadcastMessageTime, Broadcast.BroadcastFlags.Normal);
		}
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Warhead detonated.", ServerLogs.ServerLogType.GameEvent);
		if (DecontaminationController.Singleton.DecontaminationOverride != DecontaminationController.DecontaminationStatus.Disabled)
		{
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, "LCZ decontamination has been disabled by detonation of the Alpha Warhead.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging);
			DecontaminationController.Singleton.NetworkDecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
		}
		this.AlreadyDetonated = true;
		HashSet<Team> hashSet = new HashSet<Team>();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			PlayerRoleBase currentRole = allHub.roleManager.CurrentRole;
			if (allHub.IsAlive() && (!(currentRole is IFpcRole fpcRole) || AlphaWarheadController.CanBeDetonated(fpcRole.FpcModule.Position)))
			{
				hashSet.Add(allHub.GetTeam());
				allHub.playerStats.DealDamage(new WarheadDamageHandler());
				this.WarheadKills++;
			}
		}
		foreach (Scp244DeployablePickup instance in Scp244DeployablePickup.Instances)
		{
			if (AlphaWarheadController.CanBeDetonated(instance.transform.position, includeOnlyLifts: true))
			{
				instance.DestroySelf();
			}
		}
		foreach (DoorVariant allDoor in DoorVariant.AllDoors)
		{
			if (allDoor is ElevatorDoor elevatorDoor)
			{
				elevatorDoor.NetworkActiveLocks = (ushort)(elevatorDoor.ActiveLocks | 4);
			}
		}
		this.RpcShake(achieve: true);
		WarheadEvents.OnDetonated(new WarheadDetonatedEventArgs(player));
	}

	private static bool CanBeDetonated(Vector3 pos, bool includeOnlyLifts = false)
	{
		if (pos.GetZone() != FacilityZone.Surface && !includeOnlyLifts)
		{
			return true;
		}
		foreach (ElevatorChamber allChamber in ElevatorChamber.AllChambers)
		{
			if (allChamber.WorldspaceBounds.Contains(pos))
			{
				return true;
			}
		}
		return false;
	}

	private void OnConfigReloaded()
	{
		if (!AlphaWarheadController.InProgress)
		{
			return;
		}
		foreach (DoorVariant allDoor in DoorVariant.AllDoors)
		{
			if (allDoor is PryableDoor pryableDoor)
			{
				if (AlphaWarheadController.LockGatesOnCountdown)
				{
					pryableDoor.NetworkTargetState = true;
					pryableDoor.ServerChangeLock(DoorLockReason.Warhead, newState: true);
				}
				else
				{
					pryableDoor.ServerChangeLock(DoorLockReason.Warhead, newState: false);
				}
			}
		}
	}

	[ClientRpc]
	public void RpcShake(bool achieve)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteBool(achieve);
		this.SendRPCInternal("System.Void AlphaWarheadController::RpcShake(System.Boolean)", 1208415683, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void UpdateFog()
	{
	}

	[ServerCallback]
	private void ServerUpdateAutonuke()
	{
		if (NetworkServer.active && NetworkServer.active && RoundStart.RoundStarted && this._autoDetonate && !this.AlreadyDetonated && !this.Info.InProgress && !(RoundStart.RoundLength.TotalSeconds < (double)this._autoDetonateTime))
		{
			this.StartDetonation(isAutomatic: true);
		}
	}

	[ServerCallback]
	private void ServerUpdateDetonationTime()
	{
		if (!NetworkServer.active || !NetworkServer.active || !this.Info.InProgress)
		{
			return;
		}
		if (!this._blastDoorsShut && AlphaWarheadController.TimeUntilDetonation < 2f)
		{
			this._blastDoorsShut = true;
			BlastDoor.Instances.ForEach(delegate(BlastDoor x)
			{
				x.ServerSetTargetState(isOpen: false);
			});
		}
		if (this._openDoors && !this._doorsAlreadyOpen && AlphaWarheadController.TimeUntilDetonation < (float)this.CurScenario.TimeToDetonate)
		{
			this._doorsAlreadyOpen = true;
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.WarheadStart);
		}
		if (!this.AlreadyDetonated && !(AlphaWarheadController.TimeUntilDetonation > 0f))
		{
			this.Detonate();
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcShake__Boolean(bool achieve)
	{
	}

	protected static void InvokeUserCode_RpcShake__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcShake called on server.");
		}
		else
		{
			((AlphaWarheadController)obj).UserCode_RpcShake__Boolean(reader.ReadBool());
		}
	}

	static AlphaWarheadController()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AlphaWarheadController), "System.Void AlphaWarheadController::RpcShake(System.Boolean)", InvokeUserCode_RpcShake__Boolean);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteAlphaWarheadSyncInfo(this.Info);
			writer.WriteDouble(this.CooldownEndTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteAlphaWarheadSyncInfo(this.Info);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteDouble(this.CooldownEndTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Info, null, reader.ReadAlphaWarheadSyncInfo());
			base.GeneratedSyncVarDeserialize(ref this.CooldownEndTime, null, reader.ReadDouble());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Info, null, reader.ReadAlphaWarheadSyncInfo());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.CooldownEndTime, null, reader.ReadDouble());
		}
	}
}
