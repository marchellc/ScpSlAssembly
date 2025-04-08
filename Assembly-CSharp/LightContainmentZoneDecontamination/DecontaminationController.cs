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
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Spectating;
using Subtitles;
using UnityEngine;

namespace LightContainmentZoneDecontamination
{
	public class DecontaminationController : NetworkBehaviour
	{
		public static double GetServerTime
		{
			get
			{
				return NetworkTime.time - DecontaminationController.Singleton.RoundStartTime + (double)DecontaminationController.Singleton.TimeOffset;
			}
		}

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
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetLocalHub(out referenceHub))
				{
					return false;
				}
				if (this._curFunction == DecontaminationController.DecontaminationPhase.PhaseFunction.Final)
				{
					return true;
				}
				if (this._curFunction == DecontaminationController.DecontaminationPhase.PhaseFunction.GloballyAudible)
				{
					return true;
				}
				ICameraController cameraController = referenceHub.roleManager.CurrentRole as ICameraController;
				float num = ((cameraController != null) ? cameraController.CameraPosition.y : referenceHub.transform.position.y);
				return num > -200f && num < 200f;
			}
		}

		public bool IsDecontaminating
		{
			get
			{
				return NetworkServer.active && this._decontaminationBegun;
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

		private void OnElevatorTextChanged(string oldValue, string newValue)
		{
		}

		private void SetElevatorTextClient(string text)
		{
		}

		private void OnTimeOffsetChanged(float oldValue, float newValue)
		{
		}

		public void OnChangeDisableDecontamination(DecontaminationController.DecontaminationStatus oldValue, DecontaminationController.DecontaminationStatus newValue)
		{
			if (oldValue == newValue)
			{
				return;
			}
			if (newValue == DecontaminationController.DecontaminationStatus.Disabled)
			{
				DoorEventOpenerExtension.TriggerAction(DoorEventOpenerExtension.OpenerEventType.DeconReset);
				this.EnableElevators();
			}
		}

		public void ForceDecontamination()
		{
			this.NetworkDecontaminationOverride = DecontaminationController.DecontaminationStatus.Forced;
			this.FinishDecontamination();
		}

		private void Awake()
		{
			DecontaminationController.Singleton = this;
		}

		private void Start()
		{
			if (NetworkServer.active && ConfigFile.ServerConfig.GetBool("disable_decontamination", false))
			{
				this.NetworkDecontaminationOverride = DecontaminationController.DecontaminationStatus.Disabled;
			}
			for (int i = 0; i < this.DecontaminationPhases.Length; i++)
			{
				DecontaminationController.DecontaminationPhase[] decontaminationPhases = this.DecontaminationPhases;
				int num = i;
				decontaminationPhases[num].TimeTrigger = decontaminationPhases[num].TimeTrigger * 60f;
			}
		}

		private IEnumerator<float> KillPlayers()
		{
			float timer = 1f;
			while (DecontaminationController.Singleton != null && this._decontaminationBegun && this.DecontaminationOverride != DecontaminationController.DecontaminationStatus.Disabled)
			{
				timer -= Time.deltaTime;
				yield return float.NegativeInfinity;
				if (timer <= 0f)
				{
					timer = 1f;
					foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
					{
						if (referenceHub.IsAlive())
						{
							float y = referenceHub.transform.position.y;
							bool flag = y < 200f && y > -200f;
							Decontaminating effect = referenceHub.playerEffectsController.GetEffect<Decontaminating>();
							if (!effect.IsEnabled && flag)
							{
								referenceHub.playerEffectsController.EnableEffect<Decontaminating>(0f, false);
							}
							else if (effect.IsEnabled && !flag)
							{
								referenceHub.playerEffectsController.DisableEffect<Decontaminating>();
							}
						}
					}
				}
			}
			yield break;
		}

		private void FinishDecontamination()
		{
			if (NetworkServer.active)
			{
				LczDecontaminationStartingEventArgs lczDecontaminationStartingEventArgs = new LczDecontaminationStartingEventArgs();
				ServerEvents.OnLczDecontaminationStarting(lczDecontaminationStartingEventArgs);
				if (!lczDecontaminationStartingEventArgs.IsAllowed)
				{
					return;
				}
				ServerLogs.AddLog(ServerLogs.Modules.GameLogic, "Decontamination started.", ServerLogs.ServerLogType.GameEvent, false);
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
				ElevatorChamber elevatorChamber;
				if (ElevatorChamber.TryGetChamber(groupsToLock[i], out elevatorChamber))
				{
					elevatorChamber.ServerLockAllDoors(DoorLockReason.DecontLockdown, false);
				}
			}
			this._elevatorsDirty = false;
		}

		public void DisableElevators()
		{
			ElevatorGroup[] groupsToLock = DecontaminationController.GroupsToLock;
			for (int i = 0; i < groupsToLock.Length; i++)
			{
				ElevatorChamber elevatorChamber;
				if (ElevatorChamber.TryGetChamber(groupsToLock[i], out elevatorChamber))
				{
					elevatorChamber.ServerLockAllDoors(DoorLockReason.DecontLockdown, true);
					if (elevatorChamber.DestinationLevel != 1)
					{
						elevatorChamber.ServerSetDestination(1, true);
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
			if (this._stopUpdating)
			{
				return;
			}
			if (NetworkServer.active)
			{
				this.ServersideSetup();
			}
			this.UpdateTime();
		}

		private void ServersideSetup()
		{
			if (this.DecontaminationOverride != DecontaminationController.DecontaminationStatus.None)
			{
				return;
			}
			if (this.RoundStartTime != 0.0)
			{
				return;
			}
			if (RoundStart.singleton.Timer == -1)
			{
				this.NetworkRoundStartTime = NetworkTime.time;
			}
		}

		private void UpdateTime()
		{
			if (this.DecontaminationOverride != DecontaminationController.DecontaminationStatus.None)
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
			if (num != -1f && num > this.DecontaminationPhases[this._nextPhase].TimeTrigger)
			{
				if (this.DecontaminationPhases[this._nextPhase].AnnouncementLine != null && this._justJoinedCooldown >= 10f)
				{
					this._curFunction = this.DecontaminationPhases[this._nextPhase].Function;
					this.UpdateSpeaker(true);
					this.AnnouncementAudioSource.PlayOneShot(this.DecontaminationPhases[this._nextPhase].AnnouncementLine);
					if (NetworkServer.active)
					{
						List<SubtitlePart> list = new List<SubtitlePart>(1);
						switch (this._nextPhase)
						{
						case 0:
							list.Add(new SubtitlePart(SubtitleType.DecontaminationStart, null));
							break;
						case 1:
							list.Add(new SubtitlePart(SubtitleType.DecontaminationMinutes, new string[] { "10" }));
							break;
						case 2:
							list.Add(new SubtitlePart(SubtitleType.DecontaminationMinutes, new string[] { "5" }));
							break;
						case 3:
							list.Add(new SubtitlePart(SubtitleType.Decontamination1Minute, null));
							break;
						case 4:
							list.Add(new SubtitlePart(SubtitleType.DecontaminationCountdown, null));
							break;
						case 6:
							list.Add(new SubtitlePart(SubtitleType.DecontaminationLockdown, null));
							break;
						}
						foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
						{
							if (this.IsAudibleForClient(referenceHub))
							{
								new SubtitleMessage(list.ToArray()).SendToSpectatorsOf(referenceHub, true);
							}
						}
					}
				}
				if (this.DecontaminationPhases[this._nextPhase].Function == DecontaminationController.DecontaminationPhase.PhaseFunction.Final)
				{
					this.FinishDecontamination();
				}
				if (NetworkServer.active && this.DecontaminationPhases[this._nextPhase].Function == DecontaminationController.DecontaminationPhase.PhaseFunction.OpenCheckpoints)
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
		}

		private bool IsAudibleForClient(ReferenceHub hub)
		{
			if (this._curFunction == DecontaminationController.DecontaminationPhase.PhaseFunction.Final)
			{
				return true;
			}
			if (this._curFunction == DecontaminationController.DecontaminationPhase.PhaseFunction.GloballyAudible)
			{
				return true;
			}
			PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
			Scp079Role scp079Role = currentRole as Scp079Role;
			if (scp079Role != null)
			{
				return scp079Role.CurrentCamera.Room.Zone == FacilityZone.LightContainment;
			}
			IFpcRole fpcRole = currentRole as IFpcRole;
			if (fpcRole != null)
			{
				RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(fpcRole.FpcModule.Position, true);
				return roomIdentifier != null && roomIdentifier.Zone == FacilityZone.LightContainment;
			}
			return false;
		}

		private void UpdateSpeaker(bool hard)
		{
			float num = (float)(this.IsAnnouncementHearable ? 1 : 0);
			float num2 = (hard ? 1f : (Time.deltaTime * 4f));
			this.AnnouncementAudioSource.volume = Mathf.Lerp(this.AnnouncementAudioSource.volume, num, num2);
		}

		public override bool Weaved()
		{
			return true;
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
				base.GeneratedSyncVarSetter<float>(value, ref this._timeOffset, 1UL, new Action<float, float>(this.OnTimeOffsetChanged));
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
				base.GeneratedSyncVarSetter<double>(value, ref this.RoundStartTime, 2UL, null);
			}
		}

		public DecontaminationController.DecontaminationStatus NetworkDecontaminationOverride
		{
			get
			{
				return this.DecontaminationOverride;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<DecontaminationController.DecontaminationStatus>(value, ref this.DecontaminationOverride, 4UL, new Action<DecontaminationController.DecontaminationStatus, DecontaminationController.DecontaminationStatus>(this.OnChangeDisableDecontamination));
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
				base.GeneratedSyncVarSetter<string>(value, ref this._elevatorsLockedText, 8UL, new Action<string, string>(this.OnElevatorTextChanged));
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteFloat(this._timeOffset);
				writer.WriteDouble(this.RoundStartTime);
				global::Mirror.GeneratedNetworkCode._Write_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus(writer, this.DecontaminationOverride);
				writer.WriteString(this._elevatorsLockedText);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteFloat(this._timeOffset);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteDouble(this.RoundStartTime);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus(writer, this.DecontaminationOverride);
			}
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteString(this._elevatorsLockedText);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._timeOffset, new Action<float, float>(this.OnTimeOffsetChanged), reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<double>(ref this.RoundStartTime, null, reader.ReadDouble());
				base.GeneratedSyncVarDeserialize<DecontaminationController.DecontaminationStatus>(ref this.DecontaminationOverride, new Action<DecontaminationController.DecontaminationStatus, DecontaminationController.DecontaminationStatus>(this.OnChangeDisableDecontamination), global::Mirror.GeneratedNetworkCode._Read_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus(reader));
				base.GeneratedSyncVarDeserialize<string>(ref this._elevatorsLockedText, new Action<string, string>(this.OnElevatorTextChanged), reader.ReadString());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._timeOffset, new Action<float, float>(this.OnTimeOffsetChanged), reader.ReadFloat());
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<double>(ref this.RoundStartTime, null, reader.ReadDouble());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<DecontaminationController.DecontaminationStatus>(ref this.DecontaminationOverride, new Action<DecontaminationController.DecontaminationStatus, DecontaminationController.DecontaminationStatus>(this.OnChangeDisableDecontamination), global::Mirror.GeneratedNetworkCode._Read_LightContainmentZoneDecontamination.DecontaminationController/DecontaminationStatus(reader));
			}
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<string>(ref this._elevatorsLockedText, new Action<string, string>(this.OnElevatorTextChanged), reader.ReadString());
			}
		}

		private const float LowerBoundLCZ = -200f;

		private const float UpperBoundLCZ = 200f;

		public static DecontaminationController Singleton;

		private static readonly ElevatorGroup[] GroupsToLock = new ElevatorGroup[]
		{
			ElevatorGroup.LczA01,
			ElevatorGroup.LczA02,
			ElevatorGroup.LczB01,
			ElevatorGroup.LczB02
		};

		[SyncVar(hook = "OnTimeOffsetChanged")]
		private float _timeOffset;

		public DecontaminationController.DecontaminationPhase[] DecontaminationPhases;

		public AudioSource AnnouncementAudioSource;

		[SyncVar]
		public double RoundStartTime;

		[SyncVar(hook = "OnChangeDisableDecontamination")]
		public DecontaminationController.DecontaminationStatus DecontaminationOverride;

		public static bool AutoDeconBroadcastEnabled;

		public static string DeconBroadcastDeconMessage;

		public static ushort DeconBroadcastDeconMessageTime;

		private DecontaminationController.DecontaminationPhase.PhaseFunction _curFunction;

		private int _nextPhase;

		private bool _stopUpdating;

		private bool _elevatorsDirty;

		private bool _decontaminationBegun;

		private float _justJoinedCooldown;

		[SyncVar(hook = "OnElevatorTextChanged")]
		private string _elevatorsLockedText;

		public enum DecontaminationStatus : byte
		{
			None,
			Disabled,
			Forced
		}

		[Serializable]
		public struct DecontaminationPhase
		{
			public float TimeTrigger;

			public float GameTime;

			public AudioClip AnnouncementLine;

			public DecontaminationController.DecontaminationPhase.PhaseFunction Function;

			public enum PhaseFunction : byte
			{
				None,
				GloballyAudible,
				OpenCheckpoints,
				Final
			}
		}
	}
}
