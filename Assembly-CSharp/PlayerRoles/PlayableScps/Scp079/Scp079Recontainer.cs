using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MapGeneration.Distributors;
using Mirror;
using PlayerStatsSystem;
using Subtitles;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079Recontainer : MonoBehaviour
	{
		private bool CassieBusy
		{
			get
			{
				return NineTailedFoxAnnouncer.singleton.queue.Count > 0;
			}
		}

		private void Start()
		{
			this.SetContainmentDoors(false, true);
			PlayerRoleManager.OnServerRoleSet += this.OnServerRoleChanged;
		}

		private void OnDestroy()
		{
			PlayerRoleManager.OnServerRoleSet -= this.OnServerRoleChanged;
		}

		private void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.RefreshActivator();
			this.RefreshAmount();
			if (this._unlockStopwatch.IsRunning && this._unlockStopwatch.Elapsed.TotalSeconds > (double)this._lockdownDuration)
			{
				this.EndOvercharge();
				this._unlockStopwatch.Stop();
			}
			if (this._recontainLater > 0f)
			{
				this._delayStopwatch.Stop();
				if (!this.CassieBusy)
				{
					this._recontainLater -= Time.deltaTime;
				}
				if (this._recontainLater <= 0f)
				{
					this.Recontain();
				}
			}
		}

		private void OnServerRoleChanged(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (newRole != RoleTypeId.Spectator || !this.IsScpButNot079(hub.roleManager.CurrentRole))
			{
				return;
			}
			if (Scp079Role.ActiveInstances.Count == 0)
			{
				return;
			}
			if (ReferenceHub.AllHubs.Count((ReferenceHub x) => x != hub && this.IsScpButNot079(x.roleManager.CurrentRole)) > 0)
			{
				return;
			}
			this.SetContainmentDoors(true, true);
			this._alreadyRecontained = true;
			this._recontainLater = 3f;
			foreach (Scp079Generator scp079Generator in Scp079Recontainer.AllGenerators)
			{
				scp079Generator.Engaged = true;
			}
		}

		private bool IsScpButNot079(PlayerRoleBase prb)
		{
			return prb.Team == Team.SCPs && prb.RoleTypeId != RoleTypeId.Scp079;
		}

		private void RefreshActivator()
		{
			if (this._delayStopwatch.Elapsed.TotalSeconds > (double)this._activationDelay)
			{
				if (!this._delayStopwatch.IsRunning)
				{
					return;
				}
				this.BeginOvercharge();
				this._delayStopwatch.Stop();
				this._unlockStopwatch.Start();
				return;
			}
			else
			{
				if (!this._activatorGlass.isBroken)
				{
					return;
				}
				this._activatorButton.transform.localPosition = Vector3.Lerp(this._activatorButton.transform.localPosition, this._activatorPos, this._activatorLerpSpeed * Time.deltaTime);
				if (this._alreadyRecontained)
				{
					return;
				}
				if (this.CassieBusy)
				{
					return;
				}
				this.Recontain();
				return;
			}
		}

		private void Recontain()
		{
			this._delayStopwatch.Restart();
			this.PlayAnnouncement(this._announcementCountdown, 0f);
			new SubtitleMessage(new SubtitlePart[]
			{
				new SubtitlePart(SubtitleType.OverchargeIn, null)
			}).SendToAuthenticated(0);
			this._alreadyRecontained = true;
		}

		private void RefreshAmount()
		{
			if (this._alreadyRecontained)
			{
				return;
			}
			int num = 0;
			using (HashSet<Scp079Generator>.Enumerator enumerator = Scp079Recontainer.AllGenerators.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Engaged)
					{
						num++;
					}
				}
			}
			if (num > this._prevEngaged)
			{
				this.UpdateStatus(num);
				this._prevEngaged = num;
			}
		}

		private void SetContainmentDoors(bool opened, bool locked)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (DoorVariant doorVariant in this._containmentGates)
			{
				doorVariant.NetworkTargetState = opened;
				doorVariant.ServerChangeLock(DoorLockReason.SpecialDoorFeature, locked);
			}
		}

		private void UpdateStatus(int engagedGenerators)
		{
			int count = Scp079Recontainer.AllGenerators.Count;
			string text = string.Format(this._announcementProgress, engagedGenerators, count);
			List<SubtitlePart> list = new List<SubtitlePart>
			{
				new SubtitlePart(SubtitleType.GeneratorsActivated, new string[]
				{
					engagedGenerators.ToString(),
					count.ToString()
				})
			};
			if (engagedGenerators >= count)
			{
				text += this._announcementAllActivated;
				this.SetContainmentDoors(true, Scp079Role.ActiveInstances.Count > 0);
				list.Add(new SubtitlePart(SubtitleType.AllGeneratorsEngaged, null));
				DoorVariant[] containmentGates = this._containmentGates;
				for (int i = 0; i < containmentGates.Length; i++)
				{
					IScp106PassableDoor scp106PassableDoor = containmentGates[i] as IScp106PassableDoor;
					if (scp106PassableDoor != null)
					{
						scp106PassableDoor.IsScp106Passable = true;
					}
				}
			}
			new SubtitleMessage(list.ToArray()).SendToAuthenticated(0);
			this.PlayAnnouncement(text, 1f);
		}

		private void BeginOvercharge()
		{
			this._success = this.TryKill079();
			bool inProgress = AlphaWarheadController.InProgress;
			foreach (KeyValuePair<IInteractable, Dictionary<byte, InteractableCollider>> keyValuePair in InteractableCollider.AllInstances)
			{
				BasicDoor basicDoor = keyValuePair.Key as BasicDoor;
				RoomIdentifier roomIdentifier;
				if (basicDoor != null && !(basicDoor == null) && basicDoor.RequiredPermissions.RequiredPermissions == KeycardPermissions.None && RoomIdentifier.RoomsByCoordinates.TryGetValue(RoomUtils.PositionToCoords(basicDoor.transform.position), out roomIdentifier) && roomIdentifier.Zone == FacilityZone.HeavyContainment && !this._containmentGates.Contains(basicDoor))
				{
					BasicDoor basicDoor2 = basicDoor;
					basicDoor2.NetworkTargetState = basicDoor2.TargetState && inProgress;
					basicDoor.ServerChangeLock(DoorLockReason.NoPower, true);
					this._lockedDoors.Add(basicDoor);
				}
			}
			foreach (RoomLightController roomLightController in RoomLightController.Instances)
			{
				RoomIdentifier roomIdentifier2;
				if (RoomIdentifier.RoomsByCoordinates.TryGetValue(RoomUtils.PositionToCoords(roomLightController.transform.position), out roomIdentifier2) && roomIdentifier2.Zone == FacilityZone.HeavyContainment)
				{
					roomLightController.ServerFlickerLights(this._lockdownDuration);
				}
			}
			this.SetContainmentDoors(true, false);
		}

		private void EndOvercharge()
		{
			if (!this._success)
			{
				this.PlayAnnouncement(this._announcementFailure, 1f);
				new SubtitleMessage(new SubtitlePart[]
				{
					new SubtitlePart(SubtitleType.OperationalMode, null)
				}).SendToAuthenticated(0);
			}
			foreach (DoorVariant doorVariant in this._lockedDoors)
			{
				doorVariant.ServerChangeLock(DoorLockReason.NoPower, false);
				ElevatorDoor elevatorDoor = doorVariant as ElevatorDoor;
				if (elevatorDoor != null && elevatorDoor.Chamber.IsReadyForUserInput && elevatorDoor.Chamber.DestinationDoor == elevatorDoor)
				{
					doorVariant.NetworkTargetState = true;
				}
			}
		}

		private bool TryKill079()
		{
			bool flag = false;
			HashSet<ReferenceHub> hashSet = new HashSet<ReferenceHub>();
			using (HashSet<Scp079Role>.Enumerator enumerator = Scp079Role.ActiveInstances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ReferenceHub referenceHub;
					if (enumerator.Current.TryGetOwner(out referenceHub))
					{
						hashSet.Add(referenceHub);
					}
				}
			}
			foreach (ReferenceHub referenceHub2 in hashSet)
			{
				flag = true;
				if (this._activatorGlass.LastAttacker.IsSet)
				{
					referenceHub2.playerStats.DealDamage(new RecontainmentDamageHandler(this._activatorGlass.LastAttacker));
				}
				else
				{
					referenceHub2.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Recontained, null));
				}
			}
			return flag;
		}

		private void PlayAnnouncement(string annc, float glitchyMultiplier)
		{
			NineTailedFoxAnnouncer.singleton.ServerOnlyAddGlitchyPhrase(annc, 0.035f * glitchyMultiplier, 0.03f * glitchyMultiplier);
		}

		public static readonly HashSet<Scp079Generator> AllGenerators = new HashSet<Scp079Generator>();

		[SerializeField]
		private DoorVariant[] _containmentGates;

		[SerializeField]
		private float _activationDelay;

		[SerializeField]
		private float _lockdownDuration;

		[SerializeField]
		private Transform _activatorButton;

		[SerializeField]
		private BreakableWindow _activatorGlass;

		[SerializeField]
		private Vector3 _activatorPos;

		[SerializeField]
		private float _activatorLerpSpeed;

		[SerializeField]
		private string _announcementProgress;

		[SerializeField]
		private string _announcementAllActivated;

		[SerializeField]
		private string _announcementCountdown;

		[SerializeField]
		private string _announcementSuccess;

		[SerializeField]
		private string _announcementFailure;

		private const float AnnouncementGlitchChance = 0.035f;

		private const float AnnouncementJamChance = 0.03f;

		private bool _alreadyRecontained;

		private bool _success;

		private int _prevEngaged;

		private float _recontainLater;

		private readonly Stopwatch _delayStopwatch = new Stopwatch();

		private readonly Stopwatch _unlockStopwatch = new Stopwatch();

		private readonly HashSet<DoorVariant> _lockedDoors = new HashSet<DoorVariant>();
	}
}
