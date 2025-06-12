using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp049;

public class Scp049SenseAbility : KeySubroutine<Scp049Role>
{
	private const float BaseCooldown = 25f;

	private const float TargetLostCooldown = 25f;

	private const float AttemptFailCooldown = 2.5f;

	private const float EffectDuration = 20f;

	private const float HeightDiffIgnoreY = 0.1f;

	private const float NearbyDistanceSqr = 4.5f;

	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	public readonly AbilityCooldown Duration = new AbilityCooldown();

	public readonly HashSet<ReferenceHub> DeadTargets = new HashSet<ReferenceHub>();

	public readonly HashSet<ReferenceHub> SpecialZombies = new HashSet<ReferenceHub>();

	public AbilityHud SenseAbilityHUD;

	[SerializeField]
	private GameObject _effectPrefab;

	[SerializeField]
	private float _dotThreshold = 0.88f;

	[SerializeField]
	private float _distanceThreshold = 100f;

	private Scp049AttackAbility _attackAbility;

	private Transform _pulseEffect;

	private bool _hasPulseUnsafe;

	public ReferenceHub Target { get; private set; }

	public bool HasTarget { get; private set; }

	public float DistanceFromTarget { get; private set; }

	protected override ActionName TargetKey => ActionName.ToggleFlashlight;

	private bool CanSeeIndicator
	{
		get
		{
			if (base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated())
			{
				return true;
			}
			return ReferenceHub.LocalHub.GetRoleId() == RoleTypeId.Scp0492;
		}
	}

	public event Action OnFailed;

	public event Action OnSuccess;

	public void ServerLoseTarget()
	{
		this.HasTarget = false;
		this.Cooldown.Trigger(25.0);
		base.ServerSendRpc(toAll: true);
	}

	public void ServerProcessKilledPlayer(ReferenceHub hub)
	{
		if (this.HasTarget && !(this.Target != hub))
		{
			this.DeadTargets.Add(hub);
			this.SpecialZombies.Add(hub);
			this.Cooldown.Trigger(25.0);
			this.HasTarget = false;
			base.ServerSendRpc(toAll: true);
		}
	}

	public bool IsTarget(ReferenceHub hub)
	{
		if (this.HasTarget)
		{
			return hub == this.Target;
		}
		return false;
	}

	protected override void Update()
	{
		base.Update();
		if (this.HasTarget)
		{
			bool flag = false;
			if (this.Target.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				flag = true;
				Vector3 position = fpcRole.FpcModule.Position;
				Vector3 position2 = base.CastRole.FpcModule.Position;
				this.DistanceFromTarget = (position - position2).sqrMagnitude;
			}
			if (!HitboxIdentity.IsEnemy(base.Owner, this.Target))
			{
				flag = false;
			}
			if (NetworkServer.active && !(base.CastRole.VisibilityController.ValidateVisibility(this.Target) && !this.Duration.IsReady && flag))
			{
				this.ServerLoseTarget();
			}
		}
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (this.Duration.IsReady && this.Cooldown.IsReady)
		{
			if (!this.CanFindTarget(out var bestTarget))
			{
				this.Target = null;
				this.OnFailed?.Invoke();
				base.ClientSendCmd();
			}
			else
			{
				this.OnSuccess?.Invoke();
				this.Target = bestTarget;
				base.ClientSendCmd();
			}
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		base.GetSubroutine<Scp049AttackAbility>(out this._attackAbility);
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
		SpectatorTargetTracker.OnTargetChanged += OnSpectatorTargetChanged;
		this._attackAbility.OnServerHit += OnServerHit;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
		this.Duration.Clear();
		this.DeadTargets.Clear();
		this.HasTarget = false;
		PlayerRoleManager.OnRoleChanged -= OnRoleChanged;
		SpectatorTargetTracker.OnTargetChanged -= OnSpectatorTargetChanged;
		this._attackAbility.OnServerHit -= OnServerHit;
	}

	private void OnServerHit(ReferenceHub hub)
	{
		if (this.HasTarget && !(hub == this.Target))
		{
			this.ServerLoseTarget();
		}
	}

	private void OnSpectatorTargetChanged()
	{
		if (this._hasPulseUnsafe)
		{
			if (this._pulseEffect != null)
			{
				UnityEngine.Object.Destroy(this._pulseEffect.gameObject);
			}
			this._hasPulseUnsafe = false;
		}
	}

	private void OnRoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (NetworkServer.active)
		{
			if (newRole is HumanRole || newRole is ZombieRole)
			{
				this.DeadTargets.Remove(userHub);
			}
			if (prevRole is SpectatorRole && !(newRole is ZombieRole))
			{
				this.SpecialZombies.Remove(userHub);
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		if (!this.Cooldown.IsReady || !this.Duration.IsReady)
		{
			return;
		}
		this.HasTarget = false;
		this.Target = reader.ReadReferenceHub();
		if (this.Target == null)
		{
			this.Cooldown.Trigger(2.5);
			base.ServerSendRpc(toAll: true);
		}
		else
		{
			if (!HitboxIdentity.IsEnemy(base.Owner, this.Target) || !(this.Target.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase))
			{
				return;
			}
			float radius = fpcStandardRoleBase.FpcModule.CharController.radius;
			Vector3 cameraPosition = fpcStandardRoleBase.CameraPosition;
			if (VisionInformation.GetVisionInformation(base.Owner, base.Owner.PlayerCameraReference, cameraPosition, radius, this._distanceThreshold, checkFog: true, checkLineOfSight: true, 0, checkInDarkness: false).IsLooking)
			{
				Scp049UsingSenseEventArgs e = new Scp049UsingSenseEventArgs(base.Owner, this.Target);
				Scp049Events.OnUsingSense(e);
				if (e.IsAllowed)
				{
					this.Target = e.Target.ReferenceHub;
					this.Duration.Trigger(20.0);
					this.HasTarget = true;
					base.ServerSendRpc(toAll: true);
					Scp049Events.OnUsedSense(new Scp049UsedSenseEventArgs(base.Owner, this.Target));
				}
			}
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		writer.WriteReferenceHub(this.HasTarget ? this.Target : null);
		this.Cooldown.WriteCooldown(writer);
		this.Duration.WriteCooldown(writer);
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		writer.WriteReferenceHub(this.Target);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		this.Target = reader.ReadReferenceHub();
		this.HasTarget = this.Target != null;
		if (this._hasPulseUnsafe && this._pulseEffect != null)
		{
			UnityEngine.Object.Destroy(this._pulseEffect.gameObject);
			this._hasPulseUnsafe = false;
		}
		if (this.HasTarget && this.CanSeeIndicator)
		{
			this._pulseEffect = UnityEngine.Object.Instantiate(this._effectPrefab, this.Target.transform).transform;
			this._hasPulseUnsafe = true;
			UnityEngine.Object.Destroy(this._pulseEffect.gameObject, 3.5f);
		}
		this.Cooldown.ReadCooldown(reader);
		this.Duration.ReadCooldown(reader);
	}

	private bool CanFindTarget(out ReferenceHub bestTarget)
	{
		Transform playerCameraReference = base.Owner.PlayerCameraReference;
		float num = this._distanceThreshold * this._distanceThreshold;
		float num2 = this._dotThreshold;
		bool result = false;
		bestTarget = null;
		Vector3 position = base.CastRole.FpcModule.Position;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!HitboxIdentity.IsEnemy(base.Owner, allHub) || !(allHub.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase))
			{
				continue;
			}
			Vector3 position2 = fpcStandardRoleBase.FpcModule.Position;
			Vector3 vector = position2 - playerCameraReference.position;
			Vector3 forward = playerCameraReference.forward;
			if (Mathf.Abs((position2 - position).y) < 0.1f && vector.sqrMagnitude < 4.5f)
			{
				forward.y = 0f;
				forward.Normalize();
				vector.y = 0f;
			}
			float num3 = Vector3.Dot(forward, vector.normalized);
			if (num3 < num2)
			{
				continue;
			}
			float sqrMagnitude = (position2 - position).sqrMagnitude;
			if (!(sqrMagnitude > num))
			{
				float radius = fpcStandardRoleBase.FpcModule.CharacterControllerSettings.Radius;
				if (VisionInformation.GetVisionInformation(base.Owner, playerCameraReference, fpcStandardRoleBase.CameraPosition, radius, this._distanceThreshold, checkFog: true, checkLineOfSight: true, 0, checkInDarkness: false).IsLooking)
				{
					num = sqrMagnitude;
					bestTarget = allHub;
					num2 = num3;
					result = true;
				}
			}
		}
		return result;
	}
}
