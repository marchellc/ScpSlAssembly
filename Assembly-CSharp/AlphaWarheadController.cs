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
	public bool AlreadyDetonated { get; private set; }

	public AlphaWarheadController.DetonationScenario CurScenario
	{
		get
		{
			switch (this.Info.ScenarioType)
			{
			case WarheadScenarioType.Start:
				return this.StartScenarios[(int)this.Info.ScenarioId];
			case WarheadScenarioType.Resume:
				return this.ResumeScenarios[(int)this.Info.ScenarioId];
			case WarheadScenarioType.DeadmanSwitch:
				return this.DeadmanSwitchScenario;
			default:
				return null;
			}
		}
	}

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
			return AlphaWarheadController.InProgress && AlphaWarheadController.TimeUntilDetonation == 0f;
		}
	}

	public static bool InProgress
	{
		get
		{
			return AlphaWarheadController.SingletonSet && AlphaWarheadController.Singleton.Info.InProgress;
		}
	}

	public static float TimeUntilDetonation
	{
		get
		{
			return Mathf.Max(0f, (float)(AlphaWarheadController.Singleton.Info.StartTime + (double)AlphaWarheadController.Singleton.CurScenario.TotalTime - NetworkTime.time));
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
		this._autoDetonateTime = ConfigFile.ServerConfig.GetFloat("auto_warhead_start_minutes", 0f) * 60f;
		this._autoDetonate = this._autoDetonateTime > 0f;
		this._autoDetonateLock = ConfigFile.ServerConfig.GetBool("auto_warhead_lock", false);
		this._openDoors = ConfigFile.ServerConfig.GetBool("open_doors_on_countdown", true);
		this._cooldown = ConfigFile.ServerConfig.GetInt("warhead_cooldown", 40);
		AlphaWarheadSyncInfo alphaWarheadSyncInfo = default(AlphaWarheadSyncInfo);
		int @int = ConfigFile.ServerConfig.GetInt("warhead_tminus_start_duration", 90);
		alphaWarheadSyncInfo.ScenarioId = this.DefaultScenarioId;
		byte b = 0;
		while ((int)b < this.StartScenarios.Length)
		{
			if (this.StartScenarios[(int)b].TimeToDetonate == @int)
			{
				alphaWarheadSyncInfo.ScenarioId = b;
			}
			b += 1;
		}
		this.NetworkInfo = alphaWarheadSyncInfo;
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(this.OnConfigReloaded));
	}

	private void OnDestroy()
	{
		AlphaWarheadController.SingletonSet = false;
		ConfigFile.OnConfigReloaded = (Action)Delegate.Remove(ConfigFile.OnConfigReloaded, new Action(this.OnConfigReloaded));
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
		ReferenceHub referenceHub;
		return ReferenceHub.TryGetLocalHub(out referenceHub) && referenceHub.TryGetComponent<Broadcast>(out broadcaster);
	}

	private void OnInfoUpdated()
	{
		bool inProgress = this.Info.InProgress;
		if (inProgress != this._prevInfo.InProgress)
		{
			Action<bool> onProgressChanged = AlphaWarheadController.OnProgressChanged;
			if (onProgressChanged != null)
			{
				onProgressChanged(inProgress);
			}
		}
		this._alarmSource.Stop();
		if (!inProgress)
		{
			this._alarmSource.PlayOneShot(this._cancelSound);
			return;
		}
		this._alarmSource.volume = 1f;
		this._alarmSource.clip = this.CurScenario.Clip;
		float num = (float)(NetworkTime.time - this.Info.StartTime);
		if (num < 0f)
		{
			this._alarmSource.PlayDelayed(-num);
			return;
		}
		if (num < this._alarmSource.clip.length)
		{
			this._alarmSource.Play();
			this._alarmSource.time = num;
		}
	}

	public void ForceTime(float remaining)
	{
		this.InstantPrepare();
		this.StartDetonation(false, true, null);
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
		AlphaWarheadSyncInfo alphaWarheadSyncInfo = this.Info;
		alphaWarheadSyncInfo.StartTime = NetworkTime.time;
		WarheadStartingEventArgs warheadStartingEventArgs = new WarheadStartingEventArgs((trigger == null) ? ReferenceHub.HostHub : trigger, isAutomatic, suppressSubtitles, alphaWarheadSyncInfo);
		WarheadEvents.OnStarting(warheadStartingEventArgs);
		if (!warheadStartingEventArgs.IsAllowed)
		{
			return;
		}
		isAutomatic = warheadStartingEventArgs.IsAutomatic;
		suppressSubtitles = warheadStartingEventArgs.SuppressSubtitles;
		alphaWarheadSyncInfo = warheadStartingEventArgs.WarheadState;
		trigger = warheadStartingEventArgs.Player.ReferenceHub;
		this._isAutomatic = isAutomatic;
		this.AlreadyDetonated = false;
		if (isAutomatic)
		{
			this.IsLocked |= this._autoDetonateLock;
			Broadcast broadcast;
			if (!this.AlreadyDetonated && !this.Info.InProgress && AlphaWarheadController.AutoWarheadBroadcastEnabled && this.TryGetBroadcaster(out broadcast))
			{
				broadcast.RpcAddElement(AlphaWarheadController.WarheadBroadcastMessage, AlphaWarheadController.WarheadBroadcastMessageTime, Broadcast.BroadcastFlags.Normal);
			}
			this._autoDetonate = false;
		}
		this._doorsAlreadyOpen = false;
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Countdown started.", ServerLogs.ServerLogType.GameEvent, false);
		this._triggeringPlayer = new Footprint(trigger);
		this.NetworkInfo = alphaWarheadSyncInfo;
		WarheadEvents.OnStarted(new WarheadStartedEventArgs((trigger == null) ? ReferenceHub.HostHub : trigger, isAutomatic, suppressSubtitles, alphaWarheadSyncInfo));
		if (suppressSubtitles)
		{
			return;
		}
		SubtitleType subtitleType = ((this.Info.ScenarioType == WarheadScenarioType.Resume) ? SubtitleType.AlphaWarheadResumed : SubtitleType.AlphaWarheadEngage);
		new SubtitleMessage(new SubtitlePart[]
		{
			new SubtitlePart(subtitleType, new string[] { this.CurScenario.TimeToDetonate.ToString() })
		}).SendToAuthenticated(0);
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
		AlphaWarheadSyncInfo alphaWarheadSyncInfo = this.Info;
		WarheadStoppingEventArgs warheadStoppingEventArgs = new WarheadStoppingEventArgs((disabler == null) ? ReferenceHub.HostHub : disabler, alphaWarheadSyncInfo);
		WarheadEvents.OnStopping(warheadStoppingEventArgs);
		if (!warheadStoppingEventArgs.IsAllowed)
		{
			return;
		}
		alphaWarheadSyncInfo = warheadStoppingEventArgs.WarheadState;
		disabler = warheadStoppingEventArgs.Player.ReferenceHub;
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Detonation cancelled.", ServerLogs.ServerLogType.GameEvent, false);
		if (AlphaWarheadController.TimeUntilDetonation <= 15f && disabler != null)
		{
			AchievementHandlerBase.ServerAchieve(disabler.connectionToClient, AchievementName.ThatWasClose);
		}
		alphaWarheadSyncInfo.StartTime = 0.0;
		int num = (int)Mathf.Min(AlphaWarheadController.TimeUntilDetonation, (float)this.CurScenario.TimeToDetonate);
		int num2 = int.MaxValue;
		alphaWarheadSyncInfo.ScenarioType = WarheadScenarioType.Resume;
		byte b = 0;
		while ((int)b < this.ResumeScenarios.Length)
		{
			int num3 = this.ResumeScenarios[(int)b].TimeToDetonate - num;
			if (num3 >= 0 && num3 <= num2)
			{
				num2 = num3;
				alphaWarheadSyncInfo.ScenarioId = b;
			}
			b += 1;
		}
		this.NetworkInfo = alphaWarheadSyncInfo;
		this.NetworkCooldownEndTime = NetworkTime.time + (double)this._cooldown;
		DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.WarheadCancel);
		if (!NetworkServer.active)
		{
			return;
		}
		this._isAutomatic = false;
		new SubtitleMessage(new SubtitlePart[]
		{
			new SubtitlePart(SubtitleType.AlphaWarheadCancelled, null)
		}).SendToAuthenticated(0);
		WarheadEvents.OnStopped(new WarheadStoppedEventArgs((disabler == null) ? ReferenceHub.HostHub : disabler, alphaWarheadSyncInfo));
	}

	private void Detonate()
	{
		ReferenceHub referenceHub = ((this._triggeringPlayer.Hub == null) ? ReferenceHub.HostHub : this._triggeringPlayer.Hub);
		WarheadDetonatingEventArgs warheadDetonatingEventArgs = new WarheadDetonatingEventArgs(referenceHub);
		WarheadEvents.OnDetonating(warheadDetonatingEventArgs);
		if (!warheadDetonatingEventArgs.IsAllowed)
		{
			return;
		}
		this._triggeringPlayer = new Footprint(warheadDetonatingEventArgs.Player.ReferenceHub);
		Action onDetonated = AlphaWarheadController.OnDetonated;
		if (onDetonated != null)
		{
			onDetonated();
		}
		Broadcast broadcast;
		if (this._isAutomatic && !this.AlreadyDetonated && !this.Info.InProgress && AlphaWarheadController.AutoWarheadBroadcastEnabled && this.TryGetBroadcaster(out broadcast))
		{
			broadcast.RpcAddElement(AlphaWarheadController.WarheadExplodedBroadcastMessage, AlphaWarheadController.WarheadExplodedBroadcastMessageTime, Broadcast.BroadcastFlags.Normal);
		}
		ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Warhead detonated.", ServerLogs.ServerLogType.GameEvent, false);
		if (DecontaminationController.Singleton.DecontaminationOverride != DecontaminationController.DecontaminationStatus.Disabled)
		{
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, "LCZ decontamination has been disabled by detonation of the Alpha Warhead.", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			DecontaminationController.Singleton.NetworkDecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
		}
		this.AlreadyDetonated = true;
		HashSet<Team> hashSet = new HashSet<Team>();
		foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
		{
			PlayerRoleBase currentRole = referenceHub2.roleManager.CurrentRole;
			if (referenceHub2.IsAlive())
			{
				IFpcRole fpcRole = currentRole as IFpcRole;
				if (fpcRole == null || AlphaWarheadController.CanBeDetonated(fpcRole.FpcModule.Position, false))
				{
					hashSet.Add(referenceHub2.GetTeam());
					referenceHub2.playerStats.DealDamage(new WarheadDamageHandler());
					int warheadKills = this.WarheadKills;
					this.WarheadKills = warheadKills + 1;
				}
			}
		}
		foreach (Scp244DeployablePickup scp244DeployablePickup in Scp244DeployablePickup.Instances)
		{
			if (AlphaWarheadController.CanBeDetonated(scp244DeployablePickup.transform.position, true))
			{
				scp244DeployablePickup.DestroySelf();
			}
		}
		foreach (DoorVariant doorVariant in DoorVariant.AllDoors)
		{
			ElevatorDoor elevatorDoor = doorVariant as ElevatorDoor;
			if (elevatorDoor != null)
			{
				ElevatorDoor elevatorDoor2 = elevatorDoor;
				elevatorDoor2.NetworkActiveLocks = elevatorDoor2.ActiveLocks | 4;
			}
		}
		this.RpcShake(true);
		WarheadEvents.OnDetonated(new WarheadDetonatedEventArgs(referenceHub));
	}

	private static bool CanBeDetonated(Vector3 pos, bool includeOnlyLifts = false)
	{
		if (pos.y < 900f && !includeOnlyLifts)
		{
			return true;
		}
		using (List<ElevatorChamber>.Enumerator enumerator = ElevatorChamber.AllChambers.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.WorldspaceBounds.Contains(pos))
				{
					return true;
				}
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
		foreach (DoorVariant doorVariant in DoorVariant.AllDoors)
		{
			PryableDoor pryableDoor = doorVariant as PryableDoor;
			if (pryableDoor != null)
			{
				if (AlphaWarheadController.LockGatesOnCountdown)
				{
					pryableDoor.NetworkTargetState = true;
					pryableDoor.ServerChangeLock(DoorLockReason.Warhead, true);
				}
				else
				{
					pryableDoor.ServerChangeLock(DoorLockReason.Warhead, false);
				}
			}
		}
	}

	[ClientRpc]
	public void RpcShake(bool achieve)
	{
		NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
		networkWriterPooled.WriteBool(achieve);
		this.SendRPCInternal("System.Void AlphaWarheadController::RpcShake(System.Boolean)", 1208415683, networkWriterPooled, 0, true);
		NetworkWriterPool.Return(networkWriterPooled);
	}

	private void UpdateFog()
	{
	}

	[ServerCallback]
	private void ServerUpdateAutonuke()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (!NetworkServer.active || !RoundStart.RoundStarted)
		{
			return;
		}
		if (!this._autoDetonate || this.AlreadyDetonated || this.Info.InProgress)
		{
			return;
		}
		if (RoundStart.RoundLength.TotalSeconds < (double)this._autoDetonateTime)
		{
			return;
		}
		this.StartDetonation(true, false, null);
	}

	[ServerCallback]
	private void ServerUpdateDetonationTime()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (!NetworkServer.active || !this.Info.InProgress)
		{
			return;
		}
		if (!this._blastDoorsShut && AlphaWarheadController.TimeUntilDetonation < 2f)
		{
			this._blastDoorsShut = true;
			BlastDoor.Instances.ForEach(delegate(BlastDoor x)
			{
				x.ServerSetTargetState(false);
			});
		}
		if (this._openDoors && !this._doorsAlreadyOpen && AlphaWarheadController.TimeUntilDetonation < (float)this.CurScenario.TimeToDetonate)
		{
			this._doorsAlreadyOpen = true;
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.WarheadStart);
		}
		if (this.AlreadyDetonated || AlphaWarheadController.TimeUntilDetonation > 0f)
		{
			return;
		}
		this.Detonate();
	}

	public override bool Weaved()
	{
		return true;
	}

	public AlphaWarheadSyncInfo NetworkInfo
	{
		get
		{
			return this.Info;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<AlphaWarheadSyncInfo>(value, ref this.Info, 1UL, null);
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
			base.GeneratedSyncVarSetter<double>(value, ref this.CooldownEndTime, 2UL, null);
		}
	}

	protected void UserCode_RpcShake__Boolean(bool achieve)
	{
	}

	protected static void InvokeUserCode_RpcShake__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcShake called on server.");
			return;
		}
		((AlphaWarheadController)obj).UserCode_RpcShake__Boolean(reader.ReadBool());
	}

	static AlphaWarheadController()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AlphaWarheadController), "System.Void AlphaWarheadController::RpcShake(System.Boolean)", new RemoteCallDelegate(AlphaWarheadController.InvokeUserCode_RpcShake__Boolean));
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
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteAlphaWarheadSyncInfo(this.Info);
		}
		if ((base.syncVarDirtyBits & 2UL) != 0UL)
		{
			writer.WriteDouble(this.CooldownEndTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<AlphaWarheadSyncInfo>(ref this.Info, null, reader.ReadAlphaWarheadSyncInfo());
			base.GeneratedSyncVarDeserialize<double>(ref this.CooldownEndTime, null, reader.ReadDouble());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<AlphaWarheadSyncInfo>(ref this.Info, null, reader.ReadAlphaWarheadSyncInfo());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<double>(ref this.CooldownEndTime, null, reader.ReadDouble());
		}
	}

	public AlphaWarheadController.DetonationScenario[] StartScenarios;

	public AlphaWarheadController.DetonationScenario[] ResumeScenarios;

	public AlphaWarheadController.DetonationScenario DeadmanSwitchScenario;

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

	public const float FacilityDetectionThreshold = 900f;

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

	[Serializable]
	public class DetonationScenario
	{
		public int TotalTime
		{
			get
			{
				return this.TimeToDetonate + this.AdditionalTime;
			}
		}

		public AudioClip Clip;

		public int TimeToDetonate;

		public int AdditionalTime;
	}
}
