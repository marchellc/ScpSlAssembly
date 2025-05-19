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

	public static double GetServerTime => NetworkTime.time - Singleton.RoundStartTime + (double)Singleton.TimeOffset;

	public string ElevatorsLockedText
	{
		get
		{
			return _elevatorsLockedText;
		}
		set
		{
			Network_elevatorsLockedText = value;
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
			if (_curFunction == DecontaminationPhase.PhaseFunction.Final)
			{
				return true;
			}
			if (_curFunction == DecontaminationPhase.PhaseFunction.GloballyAudible)
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
				return _decontaminationBegun;
			}
			return false;
		}
	}

	public float TimeOffset
	{
		get
		{
			return _timeOffset;
		}
		set
		{
			_justJoinedCooldown = 9f;
			Network_timeOffset = value;
		}
	}

	public float Network_timeOffset
	{
		get
		{
			return _timeOffset;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref _timeOffset, 1uL, OnTimeOffsetChanged);
		}
	}

	public double NetworkRoundStartTime
	{
		get
		{
			return RoundStartTime;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref RoundStartTime, 2uL, null);
		}
	}

	public DecontaminationStatus NetworkDecontaminationOverride
	{
		get
		{
			return DecontaminationOverride;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref DecontaminationOverride, 4uL, OnChangeDisableDecontamination);
		}
	}

	public string Network_elevatorsLockedText
	{
		get
		{
			return _elevatorsLockedText;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref _elevatorsLockedText, 8uL, OnElevatorTextChanged);
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
			EnableElevators();
		}
	}

	public void ForceDecontamination()
	{
		NetworkDecontaminationOverride = DecontaminationStatus.Forced;
		FinishDecontamination();
	}

	private void Awake()
	{
		Singleton = this;
	}

	private void Start()
	{
		if (NetworkServer.active && ConfigFile.ServerConfig.GetBool("disable_decontamination"))
		{
			NetworkDecontaminationOverride = DecontaminationStatus.Disabled;
		}
		for (int i = 0; i < DecontaminationPhases.Length; i++)
		{
			DecontaminationPhases[i].TimeTrigger *= 60f;
		}
	}

	private IEnumerator<float> KillPlayers()
	{
		float timer = 1f;
		while (Singleton != null && _decontaminationBegun && DecontaminationOverride != DecontaminationStatus.Disabled)
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
		LczDecontaminationStartingEventArgs lczDecontaminationStartingEventArgs = new LczDecontaminationStartingEventArgs();
		ServerEvents.OnLczDecontaminationStarting(lczDecontaminationStartingEventArgs);
		if (lczDecontaminationStartingEventArgs.IsAllowed)
		{
			ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Decontamination started.", ServerLogs.ServerLogType.GameEvent);
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconFinish);
			DisableElevators();
			if (AutoDeconBroadcastEnabled && !_decontaminationBegun)
			{
				Broadcast.Singleton.RpcAddElement(DeconBroadcastDeconMessage, DeconBroadcastDeconMessageTime, Broadcast.BroadcastFlags.Normal);
			}
			_decontaminationBegun = true;
			Timing.RunCoroutine(KillPlayers());
			ServerEvents.OnLczDecontaminationStarted();
		}
	}

	public void EnableElevators()
	{
		ElevatorGroup[] groupsToLock = GroupsToLock;
		for (int i = 0; i < groupsToLock.Length; i++)
		{
			if (ElevatorChamber.TryGetChamber(groupsToLock[i], out var chamber))
			{
				chamber.ServerLockAllDoors(DoorLockReason.DecontLockdown, state: false);
			}
		}
		_elevatorsDirty = false;
	}

	public void DisableElevators()
	{
		ElevatorGroup[] groupsToLock = GroupsToLock;
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
		_elevatorsDirty = false;
	}

	private void Update()
	{
		if (_elevatorsDirty)
		{
			DisableElevators();
		}
		if (!_stopUpdating)
		{
			if (NetworkServer.active)
			{
				ServersideSetup();
			}
			UpdateTime();
		}
	}

	private void ServersideSetup()
	{
		if (DecontaminationOverride == DecontaminationStatus.None && RoundStartTime == 0.0 && RoundStart.singleton.Timer == -1)
		{
			NetworkRoundStartTime = NetworkTime.time;
		}
	}

	private void UpdateTime()
	{
		if (DecontaminationOverride != 0)
		{
			return;
		}
		if (RoundStartTime <= 0.0)
		{
			if (RoundStartTime == -1.0)
			{
				_stopUpdating = true;
			}
			return;
		}
		if (_justJoinedCooldown < 10f)
		{
			_justJoinedCooldown += Time.deltaTime;
		}
		float num = (float)GetServerTime;
		if (num == -1f || !(num > DecontaminationPhases[_nextPhase].TimeTrigger))
		{
			return;
		}
		if (DecontaminationPhases[_nextPhase].AnnouncementLine != null && _justJoinedCooldown >= 10f)
		{
			_curFunction = DecontaminationPhases[_nextPhase].Function;
			UpdateSpeaker(hard: true);
			AnnouncementAudioSource.PlayOneShot(DecontaminationPhases[_nextPhase].AnnouncementLine);
			if (NetworkServer.active)
			{
				List<SubtitlePart> list = new List<SubtitlePart>(1);
				switch (_nextPhase)
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
					if (IsAudibleForClient(allHub))
					{
						new SubtitleMessage(list.ToArray()).SendToSpectatorsOf(allHub, includeTarget: true);
					}
				}
			}
		}
		if (DecontaminationPhases[_nextPhase].Function == DecontaminationPhase.PhaseFunction.Final)
		{
			FinishDecontamination();
		}
		if (NetworkServer.active && DecontaminationPhases[_nextPhase].Function == DecontaminationPhase.PhaseFunction.OpenCheckpoints)
		{
			DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconEvac);
		}
		if (_nextPhase == DecontaminationPhases.Length - 1)
		{
			_stopUpdating = true;
			return;
		}
		ServerEvents.OnLczDecontaminationAnnounced(new LczDecontaminationAnnouncedEventArgs(_nextPhase));
		_nextPhase++;
	}

	private bool IsAudibleForClient(ReferenceHub hub)
	{
		if (_curFunction == DecontaminationPhase.PhaseFunction.Final)
		{
			return true;
		}
		if (_curFunction == DecontaminationPhase.PhaseFunction.GloballyAudible)
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
		float b = (IsAnnouncementHearable ? 1 : 0);
		float t = (hard ? 1f : (Time.deltaTime * 4f));
		_prevVolume = Mathf.Lerp(_prevVolume, b, t);
		AnnouncementSource.SetVolumeScale(AnnouncementAudioSource, _prevVolume);
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
			writer.WriteFloat(_timeOffset);
			writer.WriteDouble(RoundStartTime);
			GeneratedNetworkCode._Write_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(writer, DecontaminationOverride);
			writer.WriteString(_elevatorsLockedText);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteFloat(_timeOffset);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteDouble(RoundStartTime);
		}
		if ((base.syncVarDirtyBits & 4L) != 0L)
		{
			GeneratedNetworkCode._Write_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(writer, DecontaminationOverride);
		}
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteString(_elevatorsLockedText);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref _timeOffset, OnTimeOffsetChanged, reader.ReadFloat());
			GeneratedSyncVarDeserialize(ref RoundStartTime, null, reader.ReadDouble());
			GeneratedSyncVarDeserialize(ref DecontaminationOverride, OnChangeDisableDecontamination, GeneratedNetworkCode._Read_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(reader));
			GeneratedSyncVarDeserialize(ref _elevatorsLockedText, OnElevatorTextChanged, reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref _timeOffset, OnTimeOffsetChanged, reader.ReadFloat());
		}
		if ((num & 2L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref RoundStartTime, null, reader.ReadDouble());
		}
		if ((num & 4L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref DecontaminationOverride, OnChangeDisableDecontamination, GeneratedNetworkCode._Read_LightContainmentZoneDecontamination_002EDecontaminationController_002FDecontaminationStatus(reader));
		}
		if ((num & 8L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref _elevatorsLockedText, OnElevatorTextChanged, reader.ReadString());
		}
	}
}
