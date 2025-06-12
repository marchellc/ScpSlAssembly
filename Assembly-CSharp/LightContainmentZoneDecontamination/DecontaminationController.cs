using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CustomPlayerEffects;
using GameCore;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using MEC;
using Mirror;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Spectating;
using Subtitles;
using UnityEngine;

namespace LightContainmentZoneDecontamination;

public class DecontaminationController : NetworkBehaviour
{
	public enum DecontaminationStatus : byte
	{
		None,
		Disabled,
		Forced
	}

	[Serializable]
	public struct DecontaminationPhase
	{
		public enum PhaseFunction : byte
		{
			None,
			GloballyAudible,
			OpenCheckpoints,
			Final
		}

		public float TimeTrigger;

		public float GameTime;

		public AudioClip AnnouncementLine;

		public PhaseFunction Function;
	}

	public static DecontaminationController Singleton;

	private static readonly ElevatorGroup[] GroupsToLock = new ElevatorGroup[4]
	{
		ElevatorGroup.LczA01,
		ElevatorGroup.LczA02,
		ElevatorGroup.LczB01,
		ElevatorGroup.LczB02
	};

	[SyncVar(hook = "OnTimeOffsetChanged")]
	private float _timeOffset;

	public DecontaminationPhase[] DecontaminationPhases;

	public AudioSource AnnouncementAudioSource;

	[SyncVar]
	public double RoundStartTime;

	[SyncVar(hook = "OnChangeDisableDecontamination")]
	public DecontaminationStatus DecontaminationOverride;

	public static bool AutoDeconBroadcastEnabled;

	public static string DeconBroadcastDeconMessage;

	public static ushort DeconBroadcastDeconMessageTime;

	private DecontaminationPhase.PhaseFunction _curFunction;

	private int _nextPhase;

	private float _prevVolume;

	private bool _stopUpdating;

	private bool _elevatorsDirty;

	private bool _decontaminationBegun;

	private float _justJoinedCooldown;

	[SyncVar(hook = "OnElevatorTextChanged")]
	private string _elevatorsLockedText;

	public static double GetServerTime => NetworkTime.time - DecontaminationController.Singleton.RoundStartTime + (double)DecontaminationController.Singleton.TimeOffset;

	public string ElevatorsLockedText
	{
		get
		{
			return this._elevatorsLockedText;
		}
		set
		{
			this.Network_elevatorsLockedText = value;
		}
	}

	private bool IsAnnouncementHearable
	{
		get
		{
			if (!ReferenceHub.TryGetPovHub(out var hub))
			{
				return false;
			}
			if (this._curFunction == DecontaminationPhase.PhaseFunction.Final)
			{
				return true;
			}
			if (this._curFunction == DecontaminationPhase.PhaseFunction.GloballyAudible)
			{
				return true;
			}
			return hub.GetCurrentZone() == FacilityZone.LightContainment;
		}
	}

	public bool IsDecontaminating
	{
		get
		{
			if (NetworkServer.active)
			{
				return this._decontaminationBegun;
			}
			return false;
		}
	}

	public float TimeOffset
	{
		get
		{
			return this._timeOffset;
		}
		set
		{
			this._justJoinedCooldown = 9f;
			this.Network_timeOffset = value;
		}
	}

	public float Network_timeOffset
	{
		get
		{
			return this._timeOffset;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._timeOffset, 1uL, OnTimeOffsetChanged);
		}
	}

	public double NetworkRoundStartTime
	{
		get
		{
			return this.RoundStartTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.RoundStartTime, 2uL, null);
		}
	}

	public DecontaminationStatus NetworkDecontaminationOverride
	{
		get
		{
			return this.DecontaminationOverride;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.DecontaminationOverride, 4uL, OnChangeDisableDecontamination);
		}
	}

	public string Network_elevatorsLockedText
	{
		get
		{
			return this._elevatorsLockedText;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._elevatorsLockedText, 8uL, OnElevatorTextChanged);
		}
	}

	private void OnElevatorTextChanged(string oldValue, string newValue)
	{
	}

	private void SetElevatorTextClient(string text)
	{
	}

	private void OnTimeOffsetChanged(float oldValue, float newValue)
	{
	}

	public void OnChangeDisableDecontamination(DecontaminationStatus oldValue, DecontaminationStatus newValue)
	{
		if (oldValue != newValue && newValue == DecontaminationStatus.Disabled)
		{
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconReset);
			this.EnableElevators();
		}
	}

	public void ForceDecontamination()
	{
		this.NetworkDecontaminationOverride = DecontaminationStatus.Forced;
		this.FinishDecontamination();
	}

	private void Awake()
	{
		DecontaminationController.Singleton = this;
	}

	private void Start()
	{
		if (NetworkServer.active && ConfigFile.ServerConfig.GetBool("disable_decontamination"))
		{
			this.NetworkDecontaminationOverride = DecontaminationStatus.Disabled;
		}
		for (int i = 0; i < this.DecontaminationPhases.Length; i++)
		{
			this.DecontaminationPhases[i].TimeTrigger *= 60f;
		}
	}

	private IEnumerator<float> KillPlayers()
	{
		float timer = 1f;
		while (DecontaminationController.Singleton != null && this._decontaminationBegun && this.DecontaminationOverride != DecontaminationStatus.Disabled)
		{
			timer -= Time.deltaTime;
			yield return float.NegativeInfinity;
			if (!(timer <= 0f))
			{
				continue;
			}
			timer = 1f;
			foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
			{
				if (allHub.IsAlive())
				{
					bool flag = allHub.GetLastKnownZone() == FacilityZone.LightContainment;
					Decontaminating effect = allHub.playerEffectsController.GetEffect<Decontaminating>();
					if (!effect.IsEnabled && flag)
					{
						allHub.playerEffectsController.EnableEffect<Decontaminating>();
					}
					else if (effect.IsEnabled && !flag)
					{
						allHub.playerEffectsController.DisableEffect<Decontaminating>();
					}
				}
			}
		}
	}

	private void FinishDecontamination()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		LczDecontaminationStartingEventArgs e = new LczDecontaminationStartingEventArgs();
		ServerEvents.OnLczDecontaminationStarting(e);
		if (e.IsAllowed)
		{
			ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Decontamination started.", ServerLogs.ServerLogType.GameEvent);
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconFinish);
			this.DisableElevators();
			if (DecontaminationController.AutoDeconBroadcastEnabled && !this._decontaminationBegun)
			{
				Broadcast.Singleton.RpcAddElement(DecontaminationController.DeconBroadcastDeconMessage, DecontaminationController.DeconBroadcastDeconMessageTime, Broadcast.BroadcastFlags.Normal);
			}
			this._decontaminationBegun = true;
			Timing.RunCoroutine(this.KillPlayers());
			ServerEvents.OnLczDecontaminationStarted();
		}
	}

	public void EnableElevators()
	{
		ElevatorGroup[] groupsToLock = DecontaminationController.GroupsToLock;
		for (int i = 0; i < groupsToLock.Length; i++)
		{
			if (ElevatorChamber.TryGetChamber(groupsToLock[i], out var chamber))
			{
				chamber.ServerLockAllDoors(DoorLockReason.DecontLockdown, state: false);
			}
		}
		this._elevatorsDirty = false;
	}

	public void DisableElevators()
	{
		ElevatorGroup[] groupsToLock = DecontaminationController.GroupsToLock;
		for (int i = 0; i < groupsToLock.Length; i++)
		{
			if (ElevatorChamber.TryGetChamber(groupsToLock[i], out var chamber))
			{
				chamber.ServerLockAllDoors(DoorLockReason.DecontLockdown, state: true);
				if (chamber.DestinationLevel != 1)
				{
					chamber.ServerSetDestination(1, allowQueueing: true);
				}
			}
		}
		this._elevatorsDirty = false;
	}

	private void Update()
	{
		if (this._elevatorsDirty)
		{
			this.DisableElevators();
		}
		if (!this._stopUpdating)
		{
			if (NetworkServer.active)
			{
				this.ServersideSetup();
			}
			this.UpdateTime();
		}
	}

	private void ServersideSetup()
	{
		if (this.DecontaminationOverride == DecontaminationStatus.None && this.RoundStartTime == 0.0 && RoundStart.singleton.Timer == -1)
		{
			this.NetworkRoundStartTime = NetworkTime.time;
		}
	}

	private void UpdateTime()
	{
		if (this.DecontaminationOverride != DecontaminationStatus.None)
		{
			return;
		}
		if (this.RoundStartTime <= 0.0)
		{
			if (this.RoundStartTime == -1.0)
			{
				this._stopUpdating = true;
			}
			return;
		}
		if (this._justJoinedCooldown < 10f)
		{
			this._justJoinedCooldown += Time.deltaTime;
		}
		float num = (float)DecontaminationController.GetServerTime;
		if (num == -1f || !(num > this.DecontaminationPhases[this._nextPhase].TimeTrigger))
		{
			return;
		}
		if (this.DecontaminationPhases[this._nextPhase].AnnouncementLine != null && this._justJoinedCooldown >= 10f)
		{
			this._curFunction = this.DecontaminationPhases[this._nextPhase].Function;
			this.UpdateSpeaker(hard: true);
			this.AnnouncementAudioSource.PlayOneShot(this.DecontaminationPhases[this._nextPhase].AnnouncementLine);
			if (NetworkServer.active)
			{
				List<SubtitlePart> list = new List<SubtitlePart>(1);
				switch (this._nextPhase)
				{
				case 0:
					list.Add(new SubtitlePart(SubtitleType.DecontaminationStart, (string[])null));
					break;
				case 1:
					list.Add(new SubtitlePart(SubtitleType.DecontaminationMinutes, "10"));
					break;
				case 2:
					list.Add(new SubtitlePart(SubtitleType.DecontaminationMinutes, "5"));
					break;
				case 3:
					list.Add(new SubtitlePart(SubtitleType.Decontamination1Minute, (string[])null));
					break;
				case 4:
					list.Add(new SubtitlePart(SubtitleType.DecontaminationCountdown, (string[])null));
					break;
				case 6:
					list.Add(new SubtitlePart(SubtitleType.DecontaminationLockdown, (string[])null));
					break;
				}
				foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
				{
					if (this.IsAudibleForClient(allHub))
					{
						new SubtitleMessage(list.ToArray()).SendToSpectatorsOf(allHub, includeTarget: true);
					}
				}
			}
		}
		if (this.DecontaminationPhases[this._nextPhase].Function == DecontaminationPhase.PhaseFunction.Final)
		{
			this.FinishDecontamination();
		}
		if (NetworkServer.active && this.DecontaminationPhases[this._nextPhase].Function == DecontaminationPhase.PhaseFunction.OpenCheckpoints)
		{
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconEvac);
		}
		if (this._nextPhase == this.DecontaminationPhases.Length - 1)
		{
			this._stopUpdating = true;
			return;
		}
		ServerEvents.OnLczDecontaminationAnnounced(new LczDecontaminationAnnouncedEventArgs(this._nextPhase));
		this._nextPhase++;
	}

	private bool IsAudibleForClient(ReferenceHub hub)
	{
		if (this._curFunction == DecontaminationPhase.PhaseFunction.Final)
		{
			return true;
		}
		if (this._curFunction == DecontaminationPhase.PhaseFunction.GloballyAudible)
		{
			return true;
		}
		if (hub.roleManager.CurrentRole is Scp079Role scp079Role)
		{
			return scp079Role.CurrentCamera.Room.Zone == FacilityZone.LightContainment;
		}
		return hub.GetCurrentZone() == FacilityZone.LightContainment;
	}

	private void UpdateSpeaker(bool hard)
	{
		float b = (this.IsAnnouncementHearable ? 1 : 0);
		float t = (hard ? 1f : (Time.deltaTime * 4f));
		this._prevVolume = Mathf.Lerp(this._prevVolume, b, t);
		AnnouncementSource.SetVolumeScale(this.AnnouncementAudioSource, this._prevVolume);
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteFloat(this._timeOffset);
			writer.WriteDouble(this.RoundStartTime);
			GeneratedNetworkCode._Write_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(writer, this.DecontaminationOverride);
			writer.WriteString(this._elevatorsLockedText);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(this._timeOffset);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteDouble(this.RoundStartTime);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			GeneratedNetworkCode._Write_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(writer, this.DecontaminationOverride);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteString(this._elevatorsLockedText);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._timeOffset, OnTimeOffsetChanged, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.RoundStartTime, null, reader.ReadDouble());
			base.GeneratedSyncVarDeserialize(ref this.DecontaminationOverride, OnChangeDisableDecontamination, GeneratedNetworkCode._Read_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(reader));
			base.GeneratedSyncVarDeserialize(ref this._elevatorsLockedText, OnElevatorTextChanged, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._timeOffset, OnTimeOffsetChanged, reader.ReadFloat());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.RoundStartTime, null, reader.ReadDouble());
		}
		if ((num & 4L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.DecontaminationOverride, OnChangeDisableDecontamination, GeneratedNetworkCode._Read_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(reader));
		}
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._elevatorsLockedText, OnElevatorTextChanged, reader.ReadString());
		}
	}
}
