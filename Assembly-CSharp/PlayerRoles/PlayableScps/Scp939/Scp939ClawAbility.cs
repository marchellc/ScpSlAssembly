using LabApi.Events.Arguments.Scp939Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939ClawAbility : ScpAttackAbilityBase<Scp939Role>
{
	public const float BaseDamage = 40f;

	public const int DamagePenetration = 75;

	private Scp939FocusAbility _focusAbility;

	private Scp939AmnesticCloudAbility _cloudAbility;

	public override float DamageAmount => 40f;

	protected override float BaseCooldown => 0.8f;

	protected override bool CanTriggerAbility
	{
		get
		{
			if (base.CanTriggerAbility && this._focusAbility.State == 0f)
			{
				return !this._cloudAbility.TargetState;
			}
			return false;
		}
	}

	protected override DamageHandlerBase DamageHandler(float damage)
	{
		return new Scp939DamageHandler(base.CastRole, damage, Scp939DamageType.Claw);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		if (this._focusAbility.State == 0f)
		{
			base.ServerProcessCmd(reader);
		}
	}

	protected override void DamagePlayers()
	{
		int num = Mathf.Max(0, base.DetectedPlayers.Count - 1);
		ReferenceHub primaryTarget = base.DetectedPlayers.GetPrimaryTarget(base.Owner.PlayerCameraReference);
		foreach (ReferenceHub detectedPlayer in base.DetectedPlayers)
		{
			if (detectedPlayer == primaryTarget)
			{
				this.DamagePlayer(detectedPlayer, this.DamageAmount);
			}
			else
			{
				this.DamagePlayer(detectedPlayer, this.DamageAmount / (float)num);
			}
		}
	}

	protected override void DamagePlayer(ReferenceHub hub, float damage)
	{
		Scp939AttackingEventArgs e = new Scp939AttackingEventArgs(base.Owner, hub, damage);
		Scp939Events.OnAttacking(e);
		if (e.IsAllowed)
		{
			hub = e.Target.ReferenceHub;
			damage = e.Damage;
			base.DamagePlayer(hub, damage);
			Scp939Events.OnAttacked(new Scp939AttackedEventArgs(base.Owner, hub, damage));
		}
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp939FocusAbility>(out this._focusAbility);
		base.GetSubroutine<Scp939AmnesticCloudAbility>(out this._cloudAbility);
	}
}
