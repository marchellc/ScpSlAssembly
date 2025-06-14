using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096;

public class Scp096CharacterModel : AnimatedCharacterModel
{
	private static readonly int AnimatorEnragingHash = Animator.StringToHash("Enraging");

	private static readonly int AnimatorEnragedHash = Animator.StringToHash("Enraged");

	private static readonly int AnimatorChargingHash = Animator.StringToHash("Charging");

	private static readonly int AnimatorTryNotToCryHash = Animator.StringToHash("TryNotToCry");

	private static readonly int AnimatorLeftAttackHash = Animator.StringToHash("LeftAttack");

	private static readonly int AnimatorPryGateHash = Animator.StringToHash("PryGate");

	private static readonly int AnimatorCalmingHash = Animator.StringToHash("Calming");

	private static readonly int AnimatorAttackHash = Animator.StringToHash("Attack");

	[SerializeField]
	private Animator _thirdPersonAnimator;

	private Scp096Role _role;

	private Scp096AttackAbility _attackAbility;

	private Scp096RageManager _rageAbility;

	[field: SerializeField]
	public Transform Head { get; private set; }

	protected override void Update()
	{
		base.Update();
		if (!(this._rageAbility == null))
		{
			bool value = this._role.IsAbilityState(Scp096AbilityState.TryingNotToCry);
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorEnragedHash, this._rageAbility.IsEnraged);
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorEnragingHash, this._rageAbility.IsDistressed);
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorChargingHash, this._role.IsAbilityState(Scp096AbilityState.Charging));
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorTryNotToCryHash, value);
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorAttackHash, this._role.IsAbilityState(Scp096AbilityState.Attacking));
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorLeftAttackHash, this._attackAbility.LeftAttack);
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorPryGateHash, this._role.IsAbilityState(Scp096AbilityState.PryingGate));
			this._thirdPersonAnimator.SetBool(Scp096CharacterModel.AnimatorCalmingHash, this._role.IsRageState(Scp096RageState.Calming));
		}
	}

	public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		base.Setup(owner, role, localPos, localRot);
		this._role = base.OwnerHub.roleManager.CurrentRole as Scp096Role;
		this._role.SubroutineModule.TryGetSubroutine<Scp096AttackAbility>(out this._attackAbility);
		this._role.SubroutineModule.TryGetSubroutine<Scp096RageManager>(out this._rageAbility);
	}
}
