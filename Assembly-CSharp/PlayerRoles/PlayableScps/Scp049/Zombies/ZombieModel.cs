using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieModel : AnimatedCharacterModel
	{
		public Transform HeadObject { get; private set; }

		private void OnAttack()
		{
			base.Animator.SetTrigger(ZombieModel.AttackHash);
		}

		protected override void Update()
		{
			base.Update();
			float num = Mathf.Abs(base.Animator.GetFloat(ZombieModel.StrafeHash));
			base.Animator.SetLayerWeight(6, Mathf.Clamp01(num));
			float num2 = Mathf.Clamp01(this._prevConsume + Time.deltaTime * 10f * (float)(this._consumeAbility.IsInProgress ? 1 : (-1)));
			if (this._prevConsume == num2)
			{
				return;
			}
			if (this._prevConsume == 0f)
			{
				base.Animator.SetTrigger(ZombieModel.ConsumeHash);
			}
			base.Animator.SetLayerWeight(8, num2);
			this._prevConsume = num2;
		}

		public override void Setup(ReferenceHub owner, IFpcRole fpc, Vector3 localPos, Quaternion localRot)
		{
			base.Setup(owner, fpc, localPos, localRot);
			ZombieRole zombieRole = base.OwnerHub.roleManager.CurrentRole as ZombieRole;
			zombieRole.SubroutineModule.TryGetSubroutine<ZombieConsumeAbility>(out this._consumeAbility);
			zombieRole.SubroutineModule.TryGetSubroutine<ZombieAttackAbility>(out this._attackAbility);
			this._attackAbility.OnTriggered += this.OnAttack;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._attackAbility.OnTriggered -= this.OnAttack;
			this._prevConsume = 0f;
		}

		private const int StrafeLayer = 6;

		private const int ConsumeLayer = 8;

		private const float ConsumeTransitionSpeed = 10f;

		private static readonly int StrafeHash = Animator.StringToHash("Strafe");

		private static readonly int AttackHash = Animator.StringToHash("Attack");

		private static readonly int ConsumeHash = Animator.StringToHash("Eat");

		private ZombieAttackAbility _attackAbility;

		private ZombieConsumeAbility _consumeAbility;

		private float _prevConsume;
	}
}
