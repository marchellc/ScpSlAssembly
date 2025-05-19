using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096TargetsTracker : StandardSubroutine<Scp096Role>
{
	private const float Vision096InnerAngle = 0.1f;

	private const float VisionTriggerDistance = 60f;

	private const float HeadSize = 0.12f;

	private const float PostRageCooldownDuration = 10f;

	public GameObject TargetMarker;

	public readonly HashSet<ReferenceHub> Targets = new HashSet<ReferenceHub>();

	private readonly AbilityCooldown _postRageCooldown = new AbilityCooldown();

	private readonly Dictionary<ReferenceHub, GameObject> _markers = new Dictionary<ReferenceHub, GameObject>();

	private readonly HashSet<ReferenceHub> _unvalidatedTargets = new HashSet<ReferenceHub>();

	[SerializeField]
	private AudioClip _targetSound;

	private bool _sendTargetsNextFrame;

	private bool _eventsAssigned;

	public bool CanReceiveTargets => !base.CastRole.IsRageState(Scp096RageState.Calming);

	public static event Action<ReferenceHub, ReferenceHub> OnTargetAdded;

	public event Action<ReferenceHub> OnTargetAttacked;

	public static event Action<ReferenceHub, ReferenceHub> OnTargetRemoved;

	public bool AddTarget(ReferenceHub target, bool isLooking)
	{
		if (target == null || Targets.Contains(target))
		{
			return false;
		}
		Scp096AddingTargetEventArgs scp096AddingTargetEventArgs = new Scp096AddingTargetEventArgs(base.Owner, target, isLooking);
		Scp096Events.OnAddingTarget(scp096AddingTargetEventArgs);
		if (!scp096AddingTargetEventArgs.IsAllowed)
		{
			return false;
		}
		Targets.Add(target);
		if (!NetworkServer.active && !_markers.ContainsKey(target))
		{
			_markers.Add(target, UnityEngine.Object.Instantiate(TargetMarker, target.transform));
		}
		_sendTargetsNextFrame = true;
		Scp096TargetsTracker.OnTargetAdded?.Invoke(base.Owner, target);
		Scp096Events.OnAddedTarget(new Scp096AddedTargetEventArgs(base.Owner, target, isLooking));
		return true;
	}

	public bool RemoveTarget(ReferenceHub target)
	{
		if (target == null || !Targets.Remove(target))
		{
			return false;
		}
		if (_markers.TryGetValue(target, out var value))
		{
			_markers.Remove(target);
			UnityEngine.Object.Destroy(value);
		}
		_sendTargetsNextFrame = true;
		Scp096TargetsTracker.OnTargetRemoved?.Invoke(base.Owner, target);
		return true;
	}

	public void ClearAllTargets()
	{
		foreach (ReferenceHub target in Targets)
		{
			if (_markers.TryGetValue(target, out var value))
			{
				_markers.Remove(target);
				UnityEngine.Object.Destroy(value);
			}
			Scp096TargetsTracker.OnTargetRemoved?.Invoke(base.Owner, target);
		}
		_sendTargetsNextFrame = true;
		Targets.Clear();
	}

	public bool IsObservedBy(ReferenceHub target)
	{
		Vector3 position = (base.CastRole.FpcModule.CharacterModelInstance as Scp096CharacterModel).Head.position;
		if (Vector3.Dot((target.PlayerCameraReference.position - position).normalized, base.Owner.PlayerCameraReference.forward) < 0.1f)
		{
			return false;
		}
		return VisionInformation.GetVisionInformation(target, target.PlayerCameraReference, position, 0.12f, 60f).IsLooking;
	}

	public bool HasTarget(ReferenceHub target)
	{
		return Targets.Contains(target);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		Targets.ForEach(writer.WriteReferenceHub);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		_unvalidatedTargets.UnionWith(Targets);
		while (reader.Position < reader.Capacity)
		{
			if (reader.TryReadReferenceHub(out var hub) && !_unvalidatedTargets.Remove(hub))
			{
				AddTarget(hub, isLooking: false);
			}
		}
		_unvalidatedTargets.ForEach(delegate(ReferenceHub x)
		{
			RemoveTarget(x);
		});
		_unvalidatedTargets.Clear();
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		_eventsAssigned = true;
		base.Owner.playerStats.OnThisPlayerDamaged += AddTargetOnDamage;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		ClearAllTargets();
		if (_eventsAssigned)
		{
			_eventsAssigned = false;
			base.Owner.playerStats.OnThisPlayerDamaged -= AddTargetOnDamage;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		ReferenceHub.OnPlayerRemoved += CheckRemovedPlayer;
		base.CastRole.StateController.OnRageUpdate += delegate(Scp096RageState state)
		{
			if (state == Scp096RageState.Calming)
			{
				_postRageCooldown.Trigger(10.0);
				ClearAllTargets();
			}
		};
	}

	private void AddTargetOnDamage(DamageHandlerBase obj)
	{
		if (obj is AttackerDamageHandler attackerDamageHandler)
		{
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (CanReceiveTargets && !(hub == null))
			{
				AddTarget(hub, isLooking: false);
				this.OnTargetAttacked?.Invoke(hub);
			}
		}
	}

	private void OnDestroy()
	{
		ReferenceHub.OnPlayerRemoved -= CheckRemovedPlayer;
	}

	private void Update()
	{
		bool visible = base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated();
		_markers.ForEachValue(delegate(GameObject x)
		{
			x.SetActive(visible);
		});
		if (NetworkServer.active)
		{
			ServerCheckTargets();
		}
	}

	private void CheckRemovedPlayer(ReferenceHub ply)
	{
		RemoveTarget(ply);
	}

	private void ServerCheckTargets()
	{
		if (base.CastRole.IsRageState(Scp096RageState.Calming) || !_postRageCooldown.IsReady)
		{
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			UpdateTarget(allHub);
		}
		if (_sendTargetsNextFrame)
		{
			_sendTargetsNextFrame = false;
			ServerSendRpc(toAll: true);
		}
	}

	private void UpdateTarget(ReferenceHub target)
	{
		if (!HitboxIdentity.IsEnemy(base.Owner, target))
		{
			RemoveTarget(target);
		}
		else if (!base.CastRole.IsAbilityState(Scp096AbilityState.Charging) && IsObservedBy(target))
		{
			AddTarget(target, isLooking: true);
		}
	}
}
