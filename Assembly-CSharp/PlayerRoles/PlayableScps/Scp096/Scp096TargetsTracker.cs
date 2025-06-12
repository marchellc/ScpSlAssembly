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
		if (target == null || this.Targets.Contains(target))
		{
			return false;
		}
		Scp096AddingTargetEventArgs e = new Scp096AddingTargetEventArgs(base.Owner, target, isLooking);
		Scp096Events.OnAddingTarget(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		this.Targets.Add(target);
		if (!NetworkServer.active && !this._markers.ContainsKey(target))
		{
			this._markers.Add(target, UnityEngine.Object.Instantiate(this.TargetMarker, target.transform));
		}
		this._sendTargetsNextFrame = true;
		Scp096TargetsTracker.OnTargetAdded?.Invoke(base.Owner, target);
		Scp096Events.OnAddedTarget(new Scp096AddedTargetEventArgs(base.Owner, target, isLooking));
		return true;
	}

	public bool RemoveTarget(ReferenceHub target)
	{
		if (target == null || !this.Targets.Remove(target))
		{
			return false;
		}
		if (this._markers.TryGetValue(target, out var value))
		{
			this._markers.Remove(target);
			UnityEngine.Object.Destroy(value);
		}
		this._sendTargetsNextFrame = true;
		Scp096TargetsTracker.OnTargetRemoved?.Invoke(base.Owner, target);
		return true;
	}

	public void ClearAllTargets()
	{
		foreach (ReferenceHub target in this.Targets)
		{
			if (this._markers.TryGetValue(target, out var value))
			{
				this._markers.Remove(target);
				UnityEngine.Object.Destroy(value);
			}
			Scp096TargetsTracker.OnTargetRemoved?.Invoke(base.Owner, target);
		}
		this._sendTargetsNextFrame = true;
		this.Targets.Clear();
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
		return this.Targets.Contains(target);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		this.Targets.ForEach(writer.WriteReferenceHub);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this._unvalidatedTargets.UnionWith(this.Targets);
		while (reader.Position < reader.Capacity)
		{
			if (reader.TryReadReferenceHub(out var hub) && !this._unvalidatedTargets.Remove(hub))
			{
				this.AddTarget(hub, isLooking: false);
			}
		}
		this._unvalidatedTargets.ForEach(delegate(ReferenceHub x)
		{
			this.RemoveTarget(x);
		});
		this._unvalidatedTargets.Clear();
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		this._eventsAssigned = true;
		base.Owner.playerStats.OnThisPlayerDamaged += AddTargetOnDamage;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.ClearAllTargets();
		if (this._eventsAssigned)
		{
			this._eventsAssigned = false;
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
				this._postRageCooldown.Trigger(10.0);
				this.ClearAllTargets();
			}
		};
	}

	private void AddTargetOnDamage(DamageHandlerBase obj)
	{
		if (obj is AttackerDamageHandler attackerDamageHandler)
		{
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (this.CanReceiveTargets && !(hub == null))
			{
				this.AddTarget(hub, isLooking: false);
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
		this._markers.ForEachValue(delegate(GameObject x)
		{
			x.SetActive(visible);
		});
		if (NetworkServer.active)
		{
			this.ServerCheckTargets();
		}
	}

	private void CheckRemovedPlayer(ReferenceHub ply)
	{
		this.RemoveTarget(ply);
	}

	private void ServerCheckTargets()
	{
		if (base.CastRole.IsRageState(Scp096RageState.Calming) || !this._postRageCooldown.IsReady)
		{
			return;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			this.UpdateTarget(allHub);
		}
		if (this._sendTargetsNextFrame)
		{
			this._sendTargetsNextFrame = false;
			base.ServerSendRpc(toAll: true);
		}
	}

	private void UpdateTarget(ReferenceHub target)
	{
		if (!HitboxIdentity.IsEnemy(base.Owner, target))
		{
			this.RemoveTarget(target);
		}
		else if (!base.CastRole.IsAbilityState(Scp096AbilityState.Charging) && this.IsObservedBy(target))
		{
			this.AddTarget(target, isLooking: true);
		}
	}
}
