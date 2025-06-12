using System.Collections.Generic;
using CustomPlayerEffects;
using Interactables;
using Interactables.Interobjects;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096ChargeAbility : KeySubroutine<Scp096Role>
{
	private static readonly Collider[] DoorDetections = new Collider[8];

	private static readonly CachedLayerMask ClientsideDoorDetectorMask = new CachedLayerMask("Door");

	private static readonly HashSet<Collider> DisabledColliders = new HashSet<Collider>();

	public const float DefaultChargeCooldown = 5f;

	private const float DefaultChargeDuration = 1f;

	private const float DamageObjects = 750f;

	private const float DamageTarget = 90f;

	private const float DamageNonTarget = 35f;

	private const float ConcussionDurationTargets = 10f;

	private const float ConcussionDurationNonTargets = 4f;

	private Scp096HitHandler _hitHandler;

	private Scp096TargetsTracker _targetsTracker;

	private Scp096AudioPlayer _audioPlayer;

	private Transform _tr;

	[SerializeField]
	private Vector3 _detectionOffset;

	[SerializeField]
	private Vector3 _detectionExtents;

	[SerializeField]
	private AudioClip[] _soundsLethal;

	[SerializeField]
	private AudioClip[] _soundsNonLethal;

	[SerializeField]
	private float _soundDistance;

	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	public readonly AbilityCooldown Duration = new AbilityCooldown();

	public bool CanCharge
	{
		get
		{
			if (base.CastRole.IsRageState(Scp096RageState.Enraged) && base.CastRole.IsAbilityState(Scp096AbilityState.None))
			{
				return this.Cooldown.IsReady;
			}
			return false;
		}
	}

	protected override ActionName TargetKey => ActionName.Zoom;

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this.Cooldown.ReadCooldown(reader);
		this.Duration.ReadCooldown(reader);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		this.Cooldown.WriteCooldown(writer);
		this.Duration.WriteCooldown(writer);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (this.CanCharge)
		{
			Scp096ChargingEventArgs e = new Scp096ChargingEventArgs(base.Owner);
			Scp096Events.OnCharging(e);
			if (e.IsAllowed)
			{
				this._hitHandler.Clear();
				this.Duration.Trigger(1.0);
				base.CastRole.StateController.SetAbilityState(Scp096AbilityState.Charging);
				base.ServerSendRpc(toAll: true);
				Scp096Events.OnCharged(new Scp096ChargedEventArgs(base.Owner));
			}
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		this._hitHandler = new Scp096HitHandler(base.CastRole, Scp096DamageHandler.AttackType.Charge, 750f, 750f, 90f, 35f);
		this._hitHandler.OnPlayerHit += delegate(ReferenceHub ply)
		{
			ply.playerEffectsController.EnableEffect<Concussed>(this._targetsTracker.HasTarget(ply) ? 10f : 4f);
		};
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		base.ClientSendCmd();
	}

	protected override void Awake()
	{
		base.Awake();
		this._tr = base.transform;
		base.GetSubroutine<Scp096AudioPlayer>(out this._audioPlayer);
		base.GetSubroutine<Scp096TargetsTracker>(out this._targetsTracker);
		base.CastRole.StateController.OnAbilityUpdate += delegate
		{
			foreach (Collider disabledCollider in Scp096ChargeAbility.DisabledColliders)
			{
				if (!(disabledCollider == null))
				{
					disabledCollider.enabled = true;
				}
			}
			Scp096ChargeAbility.DisabledColliders.Clear();
		};
	}

	protected override void Update()
	{
		base.Update();
		if (base.CastRole.IsAbilityState(Scp096AbilityState.Charging))
		{
			if (NetworkServer.active)
			{
				this.UpdateServer();
			}
			if (base.Role.IsLocalPlayer)
			{
				this.UpdateLocalClient();
			}
		}
	}

	private void UpdateServer()
	{
		if (this.Duration.IsReady || !base.CastRole.IsRageState(Scp096RageState.Enraged))
		{
			base.CastRole.ResetAbilityState();
			this.Cooldown.Trigger(5.0);
			base.ServerSendRpc(toAll: true);
			return;
		}
		Scp096HitResult scp096HitResult = this._hitHandler.DamageBox(this._tr.TransformPoint(this._detectionOffset), this._detectionExtents, this._tr.rotation);
		if (scp096HitResult != Scp096HitResult.None)
		{
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f);
			this._audioPlayer.ServerPlayAttack(scp096HitResult);
		}
	}

	private void UpdateLocalClient()
	{
		int num = Physics.OverlapBoxNonAlloc(this._tr.TransformPoint(this._detectionOffset), this._detectionExtents, Scp096ChargeAbility.DoorDetections, this._tr.rotation, Scp096ChargeAbility.ClientsideDoorDetectorMask);
		for (int i = 0; i < num; i++)
		{
			if (Scp096ChargeAbility.DoorDetections[i].TryGetComponent<InteractableCollider>(out var component))
			{
				this.CheckDoor(component.Target as IInteractable);
			}
		}
	}

	private void CheckDoor(IInteractable inter)
	{
		if (!(inter is BreakableDoor { AllColliders: var allColliders }))
		{
			if (inter is PryableDoor door)
			{
				base.GetSubroutine<Scp096PrygateAbility>(out var sr);
				sr.ClientTryPry(door);
			}
			return;
		}
		foreach (Collider collider in allColliders)
		{
			if (collider.enabled)
			{
				collider.enabled = false;
				Scp096ChargeAbility.DisabledColliders.Add(collider);
			}
		}
	}
}
