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
		SetContainmentDoors(opened: false, locked: true);
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
		RefreshActivator();
		RefreshAmount();
		if (_unlockStopwatch.IsRunning && _unlockStopwatch.Elapsed.TotalSeconds > (double)_lockdownDuration)
		{
			EndOvercharge();
			_unlockStopwatch.Stop();
		}
		if (_recontainLater > 0f)
		{
			_delayStopwatch.Stop();
			if (!CassieBusy)
			{
				_recontainLater -= Time.deltaTime;
			}
			if (_recontainLater <= 0f)
			{
				Recontain();
			}
		}
	}

	private void OnServerRoleChanged(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
	{
		if (newRole != RoleTypeId.Spectator || !IsScpButNot079(hub.roleManager.CurrentRole) || Scp079Role.ActiveInstances.Count == 0 || ReferenceHub.AllHubs.Count((ReferenceHub x) => x != hub && IsScpButNot079(x.roleManager.CurrentRole)) > 0)
		{
			return;
		}
		SetContainmentDoors(opened: true, locked: true);
		_alreadyRecontained = true;
		_recontainLater = 3f;
		foreach (Scp079Generator allGenerator in AllGenerators)
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
		if (_delayStopwatch.Elapsed.TotalSeconds > (double)_activationDelay)
		{
			if (_delayStopwatch.IsRunning)
			{
				BeginOvercharge();
				_delayStopwatch.Stop();
				_unlockStopwatch.Start();
			}
		}
		else if (_activatorGlass.isBroken)
		{
			_activatorButton.transform.localPosition = Vector3.Lerp(_activatorButton.transform.localPosition, _activatorPos, _activatorLerpSpeed * Time.deltaTime);
			if (!_alreadyRecontained && !CassieBusy)
			{
				Recontain();
			}
		}
	}

	private void Recontain()
	{
		_delayStopwatch.Restart();
		PlayAnnouncement(_announcementCountdown, 0f);
		new SubtitleMessage(new SubtitlePart(SubtitleType.OverchargeIn, (string[])null)).SendToAuthenticated();
		_alreadyRecontained = true;
	}

	private void RefreshAmount()
	{
		if (_alreadyRecontained)
		{
			return;
		}
		int num = 0;
		foreach (Scp079Generator allGenerator in AllGenerators)
		{
			if (allGenerator.Engaged)
			{
				num++;
			}
		}
		if (num > _prevEngaged)
		{
			UpdateStatus(num);
			_prevEngaged = num;
		}
	}

	private void SetContainmentDoors(bool opened, bool locked)
	{
		if (NetworkServer.active)
		{
			DoorVariant[] containmentGates = _containmentGates;
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
		int count = AllGenerators.Count;
		string text = string.Format(_announcementProgress, engagedGenerators, count);
		List<SubtitlePart> list = new List<SubtitlePart>();
		list.Add(new SubtitlePart(SubtitleType.GeneratorsActivated, engagedGenerators.ToString(), count.ToString()));
		List<SubtitlePart> list2 = list;
		if (engagedGenerators >= count)
		{
			text += _announcementAllActivated;
			SetContainmentDoors(opened: true, Scp079Role.ActiveInstances.Count > 0);
			list2.Add(new SubtitlePart(SubtitleType.AllGeneratorsEngaged, (string[])null));
			DoorVariant[] containmentGates = _containmentGates;
			for (int i = 0; i < containmentGates.Length; i++)
			{
				if (containmentGates[i] is IScp106PassableDoor scp106PassableDoor)
				{
					scp106PassableDoor.IsScp106Passable = true;
				}
			}
		}
		new SubtitleMessage(list2.ToArray()).SendToAuthenticated();
		PlayAnnouncement(text, 1f);
	}

	private void BeginOvercharge()
	{
		_success = TryKill079();
		bool inProgress = AlphaWarheadController.InProgress;
		foreach (KeyValuePair<IInteractable, Dictionary<byte, InteractableCollider>> allInstance in InteractableCollider.AllInstances)
		{
			if (allInstance.Key is BasicDoor basicDoor && !(basicDoor == null) && basicDoor.RequiredPermissions.RequiredPermissions == DoorPermissionFlags.None && basicDoor.Rooms.Any((RoomIdentifier x) => x.Zone == FacilityZone.HeavyContainment) && !_containmentGates.Contains(basicDoor))
			{
				basicDoor.NetworkTargetState = basicDoor.TargetState && inProgress;
				basicDoor.ServerChangeLock(DoorLockReason.NoPower, newState: true);
				_lockedDoors.Add(basicDoor);
			}
		}
		foreach (RoomLightController instance in RoomLightController.Instances)
		{
			if (instance.Room.Zone == FacilityZone.HeavyContainment)
			{
				instance.ServerFlickerLights(_lockdownDuration);
			}
		}
		SetContainmentDoors(opened: true, locked: false);
	}

	private void EndOvercharge()
	{
		if (!_success)
		{
			PlayAnnouncement(_announcementFailure, 1f);
			new SubtitleMessage(new SubtitlePart(SubtitleType.OperationalMode, (string[])null)).SendToAuthenticated();
		}
		foreach (DoorVariant lockedDoor in _lockedDoors)
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
			Scp079RecontainingEventArgs scp079RecontainingEventArgs = new Scp079RecontainingEventArgs(item, _activatorGlass.LastAttacker.Hub);
			Scp079Events.OnRecontaining(scp079RecontainingEventArgs);
			if (scp079RecontainingEventArgs.IsAllowed)
			{
				result = true;
				if (_activatorGlass.LastAttacker.IsSet)
				{
					item.playerStats.DealDamage(new RecontainmentDamageHandler(_activatorGlass.LastAttacker));
				}
				else
				{
					item.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Recontained));
				}
				Scp079Events.OnRecontained(new Scp079RecontainedEventArgs(item, _activatorGlass.LastAttacker.Hub));
			}
		}
		return result;
	}

	private void PlayAnnouncement(string annc, float glitchyMultiplier)
	{
		NineTailedFoxAnnouncer.singleton.ServerOnlyAddGlitchyPhrase(annc, 0.035f * glitchyMultiplier, 0.03f * glitchyMultiplier);
	}
}
