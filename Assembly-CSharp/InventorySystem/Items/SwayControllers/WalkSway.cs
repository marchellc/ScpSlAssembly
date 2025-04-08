using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.SwayControllers
{
	public class WalkSway : GoopSway
	{
		private float ScaledWalkCycle
		{
			get
			{
				IFpcRole fpcRole = this.Owner.roleManager.CurrentRole as IFpcRole;
				if (fpcRole == null)
				{
					return 0f;
				}
				AnimatedCharacterModel animatedCharacterModel = fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
				if (animatedCharacterModel == null)
				{
					return 0f;
				}
				float num = this._walkSwayCycleScale * animatedCharacterModel.WalkCycleRaw;
				if (!float.IsNaN(num))
				{
					return num - (float)((int)num);
				}
				return 0f;
			}
		}

		private float NormalizedRunningSpeed
		{
			get
			{
				IFpcRole fpcRole = this.Owner.roleManager.CurrentRole as IFpcRole;
				if (fpcRole == null)
				{
					return 0f;
				}
				FirstPersonMovementModule fpcModule = fpcRole.FpcModule;
				float num = fpcModule.VelocityForState(PlayerMovementState.Sprinting, false);
				float num2 = fpcModule.Motor.Velocity.MagnitudeIgnoreY();
				if (num <= 0f)
				{
					return 0f;
				}
				return Mathf.Clamp01(num2 / num);
			}
		}

		private bool IsJumping
		{
			get
			{
				return !this.Owner.IsGrounded();
			}
		}

		protected virtual float JumpSwayWeightMultiplier
		{
			get
			{
				return this._jumpSwayWeightMultiplier;
			}
		}

		protected virtual float WalkSwayWeightMultiplier
		{
			get
			{
				return this._walkSwayWeightMultiplier;
			}
		}

		public WalkSway(GoopSway.GoopSwaySettings hipSettings, AnimatedViewmodelBase vm)
			: base(hipSettings, vm.Hub)
		{
			this._viewmodel = vm;
			this._supportsAnimSway = this.TryInitAnimSway(out this._walkLayer, out this._walkSwayWeightMultiplier, out this._walkStateHash, out this._jumpLayer, out this._jumpSwayWeightMultiplier, out this._walkSwayCycleScale);
		}

		private bool TryInitAnimSway(out int walkLayer, out float walkWeight, out int walkStateHash, out int jumpLayer, out float jumpWeight, out float cycleScale)
		{
			walkLayer = 0;
			walkWeight = 0f;
			walkStateHash = 0;
			jumpLayer = 0;
			jumpWeight = 0f;
			cycleScale = 0f;
			int num = this._viewmodel.AnimatorGetLayerCount();
			bool flag = false;
			bool flag2 = false;
			for (int i = num - 1; i >= 0; i--)
			{
				string text = this._viewmodel.AnimatorGetLayerName(i);
				if (!(text == "Sway Walk"))
				{
					if (text == "Sway Jump")
					{
						flag2 = true;
						jumpLayer = i;
						jumpWeight = this._viewmodel.AnimatorGetLayerWeight(i);
					}
				}
				else
				{
					flag = true;
					walkLayer = i;
					walkWeight = this._viewmodel.AnimatorGetLayerWeight(i);
					AnimatorStateInfo animatorStateInfo = this._viewmodel.AnimatorStateInfo(i);
					walkStateHash = animatorStateInfo.shortNameHash;
					cycleScale = animatorStateInfo.speed;
				}
				if (flag && flag2)
				{
					return true;
				}
			}
			return false;
		}

		public override void UpdateSway()
		{
			base.UpdateSway();
			if (!this._supportsAnimSway)
			{
				return;
			}
			float num = Mathf.Clamp01(this.NormalizedRunningSpeed);
			float num2 = Mathf.MoveTowards(this._prevWalkWeight, num, Time.deltaTime * 6f);
			float num3 = Mathf.Lerp(this._prevWalkParam, num, Time.deltaTime * 4f);
			this._prevWalkWeight = num2;
			this._prevWalkParam = num3;
			this._viewmodel.AnimatorSetLayerWeight(this._walkLayer, num2 * this.WalkSwayWeightMultiplier);
			this._viewmodel.AnimatorSetLayerWeight(this._jumpLayer, this.JumpSwayWeightMultiplier);
			this._viewmodel.AnimatorSetBool(WalkSway.SwayJumpingHash, this.IsJumping);
			this._viewmodel.AnimatorSetFloat(WalkSway.SwayWalkHash, num3);
			this._viewmodel.AnimatorPlay(this._walkStateHash, this._walkLayer, this.ScaledWalkCycle);
		}

		private const float LayerAdjustSpeed = 6f;

		private const float ParamAdjustLerp = 4f;

		private const string WalkSwayLayerName = "Sway Walk";

		private const string JumpSwayLayerName = "Sway Jump";

		public static readonly int SwayWalkHash = Animator.StringToHash("SwayWalk");

		public static readonly int SwayJumpingHash = Animator.StringToHash("SwayJumping");

		private readonly bool _supportsAnimSway;

		private readonly int _walkStateHash;

		private readonly int _walkLayer;

		private readonly float _walkSwayWeightMultiplier;

		private readonly float _walkSwayCycleScale;

		private readonly int _jumpLayer;

		private readonly float _jumpSwayWeightMultiplier;

		private readonly AnimatedViewmodelBase _viewmodel;

		private float _prevWalkWeight;

		private float _prevWalkParam;
	}
}
