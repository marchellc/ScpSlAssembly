using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Interactables;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using MapGeneration.Distributors;
using Mirror;
using PlayerStatsSystem;
using Subtitles;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079Recontainer : MonoBehaviour
{
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

	private bool CassieBusy => NineTailedFoxAnnouncer.singleton.queue.Count > 0;

	private void Start()
	{
		this.SetContainmentDoors(opened: false, locked: true);
		PlayerRoleManager.OnServerRoleSet += OnServerRoleChanged;
	}

	private void OnDestroy()
	{
		PlayerRoleManager.OnServerRoleSet -= OnServerRoleChanged;
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
		if (newRole != RoleTypeId.Spectator || !this.IsScpButNot079(hub.roleManager.CurrentRole) || Scp079Role.ActiveInstances.Count == 0 || ReferenceHub.AllHubs.Count((ReferenceHub x) => x != hub && this.IsScpButNot079(x.roleManager.CurrentRole)) > 0)
		{
			return;
		}
		this.SetContainmentDoors(opened: true, locked: true);
		this._alreadyRecontained = true;
		this._recontainLater = 3f;
		foreach (Scp079Generator allGenerator in Scp079Recontainer.AllGenerators)
		{
			allGenerator.Engaged = true;
		}
	}

	private bool IsScpButNot079(PlayerRoleBase prb)
	{
		if (prb.Team == Team.SCPs)
		{
			return prb.RoleTypeId != RoleTypeId.Scp079;
		}
		return false;
	}

	private void RefreshActivator()
	{
		if (this._delayStopwatch.Elapsed.TotalSeconds > (double)this._activationDelay)
		{
			if (this._delayStopwatch.IsRunning)
			{
				this.BeginOvercharge();
				this._delayStopwatch.Stop();
				this._unlockStopwatch.Start();
			}
		}
		else if (this._activatorGlass.isBroken)
		{
			this._activatorButton.transform.localPosition = Vector3.Lerp(this._activatorButton.transform.localPosition, this._activatorPos, this._activatorLerpSpeed * Time.deltaTime);
			if (!this._alreadyRecontained && !this.CassieBusy)
			{
				this.Recontain();
			}
		}
	}

	private void Recontain()
	{
		this._delayStopwatch.Restart();
		this.PlayAnnouncement(this._announcementCountdown, 0f);
		new SubtitleMessage(new SubtitlePart(SubtitleType.OverchargeIn, (string[])null)).SendToAuthenticated();
		this._alreadyRecontained = true;
	}

	private void RefreshAmount()
	{
		if (this._alreadyRecontained)
		{
			return;
		}
		int num = 0;
		foreach (Scp079Generator allGenerator in Scp079Recontainer.AllGenerators)
		{
			if (allGenerator.Engaged)
			{
				num++;
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
		if (NetworkServer.active)
		{
			DoorVariant[] containmentGates = this._containmentGates;
			foreach (DoorVariant obj in containmentGates)
			{
				obj.NetworkTargetState = opened;
				obj.ServerChangeLock(DoorLockReason.SpecialDoorFeature, locked);
			}
		}
	}

	private void UpdateStatus(int engagedGenerators)
	{
		if (AlphaWarheadController.Detonated)
		{
			return;
		}
		int count = Scp079Recontainer.AllGenerators.Count;
		string text = string.Format(this._announcementProgress, engagedGenerators, count);
		List<SubtitlePart> list = new List<SubtitlePart>();
		list.Add(new SubtitlePart(SubtitleType.GeneratorsActivated, engagedGenerators.ToString(), count.ToString()));
		List<SubtitlePart> list2 = list;
		if (engagedGenerators >= count)
		{
			text += this._announcementAllActivated;
			this.SetContainmentDoors(opened: true, Scp079Role.ActiveInstances.Count > 0);
			list2.Add(new SubtitlePart(SubtitleType.AllGeneratorsEngaged, (string[])null));
			DoorVariant[] containmentGates = this._containmentGates;
			for (int i = 0; i < containmentGates.Length; i++)
			{
				if (containmentGates[i] is IScp106PassableDoor scp106PassableDoor)
				{
					scp106PassableDoor.IsScp106Passable = true;
				}
			}
		}
		new SubtitleMessage(list2.ToArray()).SendToAuthenticated();
		this.PlayAnnouncement(text, 1f);
	}

	private void BeginOvercharge()
	{
		this._success = this.TryKill079();
		bool inProgress = AlphaWarheadController.InProgress;
		foreach (KeyValuePair<IInteractable, Dictionary<byte, InteractableCollider>> allInstance in InteractableCollider.AllInstances)
		{
			if (allInstance.Key is BasicDoor basicDoor && !(basicDoor == null) && basicDoor.RequiredPermissions.RequiredPermissions == DoorPermissionFlags.None && basicDoor.Rooms.Any((RoomIdentifier x) => x.Zone == FacilityZone.HeavyContainment) && !this._containmentGates.Contains(basicDoor))
			{
				basicDoor.NetworkTargetState = basicDoor.TargetState && inProgress;
				basicDoor.ServerChangeLock(DoorLockReason.NoPower, newState: true);
				this._lockedDoors.Add(basicDoor);
			}
		}
		foreach (RoomLightController instance in RoomLightController.Instances)
		{
			if (instance.Room.Zone == FacilityZone.HeavyContainment)
			{
				instance.ServerFlickerLights(this._lockdownDuration);
			}
		}
		this.SetContainmentDoors(opened: true, locked: false);
	}

	private void EndOvercharge()
	{
		if (!this._success)
		{
			this.PlayAnnouncement(this._announcementFailure, 1f);
			new SubtitleMessage(new SubtitlePart(SubtitleType.OperationalMode, (string[])null)).SendToAuthenticated();
		}
		foreach (DoorVariant lockedDoor in this._lockedDoors)
		{
			lockedDoor.ServerChangeLock(DoorLockReason.NoPower, newState: false);
			if (lockedDoor is ElevatorDoor elevatorDoor && elevatorDoor.Chamber.IsReadyForUserInput && elevatorDoor.Chamber.DestinationDoor == elevatorDoor)
			{
				lockedDoor.NetworkTargetState = true;
			}
		}
	}

	private bool TryKill079()
	{
		bool result = false;
		HashSet<ReferenceHub> hashSet = new HashSet<ReferenceHub>();
		foreach (Scp079Role activeInstance in Scp079Role.ActiveInstances)
		{
			if (activeInstance.TryGetOwner(out var hub))
			{
				hashSet.Add(hub);
			}
		}
		foreach (ReferenceHub item in hashSet)
		{
			Scp079RecontainingEventArgs e = new Scp079RecontainingEventArgs(item, this._activatorGlass.LastAttacker.Hub);
			Scp079Events.OnRecontaining(e);
			if (e.IsAllowed)
			{
				result = true;
				if (this._activatorGlass.LastAttacker.IsSet)
				{
					item.playerStats.DealDamage(new RecontainmentDamageHandler(this._activatorGlass.LastAttacker));
				}
				else
				{
					item.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Recontained));
				}
				Scp079Events.OnRecontained(new Scp079RecontainedEventArgs(item, this._activatorGlass.LastAttacker.Hub));
			}
		}
		return result;
	}

	private void PlayAnnouncement(string annc, float glitchyMultiplier)
	{
		NineTailedFoxAnnouncer.singleton.ServerOnlyAddGlitchyPhrase(annc, 0.035f * glitchyMultiplier, 0.03f * glitchyMultiplier);
	}
}
