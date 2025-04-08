using System;
using InventorySystem.Items;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps.HUDs;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114FpsAnimator : ScpViewmodelBase
	{
		private float HideHandsWeight
		{
			get
			{
				if (!this._scpRole.Disguised && !this._dance.ThirdpersonMode)
				{
					return 0f;
				}
				return 1f;
			}
		}

		public override float CamFOV
		{
			get
			{
				ItemBase curInstance = base.Owner.inventory.CurInstance;
				if (!(curInstance != null) || !(curInstance.ViewModel != null))
				{
					return this._defaultFov;
				}
				return curInstance.ViewModel.ViewmodelCameraFOV;
			}
		}

		protected override void Start()
		{
			base.Start();
			this._scpRole = base.Role as Scp3114Role;
			this._scpRole.SubroutineModule.TryGetSubroutine<Scp3114Slap>(out this._slap);
			this._scpRole.SubroutineModule.TryGetSubroutine<Scp3114Dance>(out this._dance);
			this._scpRole.SubroutineModule.TryGetSubroutine<Scp3114Strangle>(out this._strangle);
			this._slap.OnTriggered += this.PlayAttackAnim;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (this._slap != null)
			{
				this._slap.OnTriggered -= this.PlayAttackAnim;
			}
		}

		protected override void UpdateAnimations()
		{
			FirstPersonMovementModule fpcModule = this._scpRole.FpcModule;
			bool isGrounded = fpcModule.IsGrounded;
			float num = fpcModule.Motor.Velocity.MagnitudeIgnoreY();
			float walkCycle = (fpcModule.CharacterModelInstance as AnimatedCharacterModel).WalkCycle;
			base.Anim.SetLayerWeight(this._hideHandsLayer, this.HideHandsWeight);
			base.Anim.SetInteger(Scp3114FpsAnimator.StatusHash, (int)this._scpRole.CurIdentity.Status);
			base.Anim.SetBool(Scp3114FpsAnimator.GroundedHash, isGrounded);
			base.Anim.SetFloat(Scp3114FpsAnimator.WalkBlendHash, isGrounded ? num : 0f, this._animDampTime, Time.deltaTime);
			base.Anim.SetFloat(Scp3114FpsAnimator.WalkCycleHash, walkCycle);
			base.Anim.SetBool(Scp3114FpsAnimator.StrangleHash, this._strangle.SyncTarget != null);
		}

		private void PlayAttackAnim()
		{
			int num;
			do
			{
				num = Mathf.FloorToInt(global::UnityEngine.Random.Range(this._attackVariantMinMax.x, this._attackVariantMinMax.y));
			}
			while (num == this._prevRand);
			base.Anim.SetFloat(Scp3114FpsAnimator.VariantHash, Mathf.Floor((float)num));
			base.Anim.SetTrigger(Scp3114FpsAnimator.AttackHash);
			this._prevRand = num;
		}

		[SerializeField]
		private float _defaultFov;

		[SerializeField]
		private Vector2 _attackVariantMinMax;

		[SerializeField]
		private int _hideHandsLayer;

		[SerializeField]
		private float _animDampTime;

		private Scp3114Slap _slap;

		private Scp3114Role _scpRole;

		private Scp3114Strangle _strangle;

		private Scp3114Dance _dance;

		private int _prevRand;

		private static readonly int AttackHash = Animator.StringToHash("Attack");

		private static readonly int VariantHash = Animator.StringToHash("AttackVariant");

		private static readonly int StatusHash = Animator.StringToHash("IdentityStatus");

		private static readonly int WalkCycleHash = Animator.StringToHash("WalkCycle");

		private static readonly int WalkBlendHash = Animator.StringToHash("WalkBlend");

		private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

		private static readonly int StrangleHash = Animator.StringToHash("Strangling");
	}
}
