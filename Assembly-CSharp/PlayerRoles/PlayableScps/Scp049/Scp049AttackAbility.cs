using System;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp049;

public class Scp049AttackAbility : KeySubroutine<Scp049Role>
{
	private const float CooldownTime = 1.5f;

	private const float LagBacktrackingCompensation = 0.4f;

	private static int _attackLayerMask;

	private const float AttackDistance = 1.728f;

	[SerializeField]
	private float _statusEffectDuration = 20f;

	private bool _isInstaKillAttack;

	private bool _isTarget;

	private ReferenceHub _target;

	private Scp049ResurrectAbility _resurrect;

	private Scp049SenseAbility _sense;

	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	public AbilityHud AttackAbilityHUD;

	internal static LayerMask AttackMask
	{
		get
		{
			if (Scp049AttackAbility._attackLayerMask == 0)
			{
				Scp049AttackAbility._attackLayerMask = LayerMask.GetMask("Hitbox") | (int)FpcStateProcessor.Mask;
			}
			return Scp049AttackAbility._attackLayerMask;
		}
	}

	protected override ActionName TargetKey => ActionName.Shoot;

	public event Action<ReferenceHub> OnServerHit;

	public override void ServerProcessCmd(NetworkReader reader)
	{
		if (!this.Cooldown.IsReady || this._resurrect.IsInProgress)
		{
			return;
		}
		this._target = reader.ReadReferenceHub();
		if (!(this._target == null) && this.IsTargetValid(this._target))
		{
			this.Cooldown.Trigger(1.5);
			CardiacArrest effect = this._target.playerEffectsController.GetEffect<CardiacArrest>();
			this._isInstaKillAttack = effect.IsEnabled;
			this._isTarget = this._sense.IsTarget(this._target);
			if (effect.IsEnabled)
			{
				this._target.playerStats.DealDamage(new Scp049DamageHandler(base.Owner, -1f, Scp049DamageHandler.AttackType.Instakill));
			}
			else
			{
				effect.SetAttacker(base.Owner);
				effect.Intensity = 1;
				effect.ServerChangeDuration(this._statusEffectDuration);
			}
			this.OnServerHit?.Invoke(this._target);
			base.ServerSendRpc(toAll: true);
			Hitmarker.SendHitmarkerDirectly(base.Owner, 1f, playAudio: false);
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		this.Cooldown.WriteCooldown(writer);
		writer.WriteBool(this._isInstaKillAttack);
		writer.WriteBool(this._isTarget);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		this.Cooldown.ReadCooldown(reader);
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		writer.WriteReferenceHub(this._target);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp049ResurrectAbility>(out this._resurrect);
		base.GetSubroutine<Scp049SenseAbility>(out this._sense);
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (this.Cooldown.IsReady && this.CanFindTarget(base.Owner.PlayerCameraReference, out this._target))
		{
			base.ClientSendCmd();
		}
	}

	private bool IsTargetValid(ReferenceHub target)
	{
		if (!(target.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		if (!HitboxIdentity.IsEnemy(base.Owner, target))
		{
			return false;
		}
		if (base.Owner.isLocalPlayer)
		{
			return true;
		}
		Bounds bounds = fpcRole.FpcModule.Tracer.GenerateBounds(0.4f, ignoreTeleports: true);
		Vector3 position = base.CastRole.FpcModule.Position;
		Vector3 vector = bounds.ClosestPoint(position);
		if (Vector3.Distance(position, vector) >= 1.728f)
		{
			return false;
		}
		return !Physics.Linecast(position, vector, FpcStateProcessor.Mask);
	}

	private bool CanFindTarget(Transform camera, out ReferenceHub target)
	{
		target = null;
		if (!Physics.Raycast(camera.position, camera.forward, out var hitInfo, 1.728f, Scp049AttackAbility.AttackMask))
		{
			return false;
		}
		if (!hitInfo.collider.TryGetComponent<HitboxIdentity>(out var component))
		{
			return false;
		}
		target = component.TargetHub;
		return true;
	}
}
