using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106Model : AnimatedCharacterModel
	{
		public override bool FootstepPlayable
		{
			get
			{
				return base.FpcModule.IsGrounded && base.FpcModule.Motor.MovementDetected && this.LandingFootstepPlayable;
			}
		}

		public override bool LandingFootstepPlayable
		{
			get
			{
				return this._sinkhole.SubmergeProgress == 0f;
			}
		}

		protected override Vector3 ModelPositionOffset
		{
			get
			{
				return base.ModelPositionOffset + this._modelOffset;
			}
		}

		private void LateUpdate()
		{
			if (base.Pooled)
			{
				return;
			}
			bool targetSubmerged = this._sinkhole.TargetSubmerged;
			float targetTransitionDuration = this._sinkhole.TargetTransitionDuration;
			float num = (targetSubmerged ? 3.333f : 3.908f);
			if (targetTransitionDuration > 0f)
			{
				base.Animator.SetFloat(Scp106Model.SpeedHash, num / targetTransitionDuration);
			}
			bool isHidden = this._sinkhole.IsHidden;
			GameObject[] hiddenObjects = this._hiddenObjects;
			for (int i = 0; i < hiddenObjects.Length; i++)
			{
				hiddenObjects[i].SetActive(!isHidden);
			}
			if (base.IsTracked)
			{
				this.SetVisibility(this._sinkhole.IsDuringAnimation);
			}
			base.Animator.SetBool(Scp106Model.SubmergeHash, targetSubmerged);
			float submergeProgress = this._sinkhole.SubmergeProgress;
			AnimationCurve animationCurve = (targetSubmerged ? this._submergeAnim : this._appearAnim);
			this._modelOffset = Vector3.up * animationCurve.Evaluate(submergeProgress);
		}

		protected override void Awake()
		{
			base.Awake();
			this._tr = base.transform;
		}

		public override void Setup(ReferenceHub owner, IFpcRole fpc, Vector3 localPos, Quaternion localRot)
		{
			base.Setup(owner, fpc, localPos, localRot);
			Scp106Role scp106Role = base.OwnerHub.roleManager.CurrentRole as Scp106Role;
			scp106Role.SubroutineModule.TryGetSubroutine<Scp106StalkAbility>(out this._stalkAbility);
			this._fpc = scp106Role.FpcModule as Scp106MovementModule;
			this._sinkhole = scp106Role.Sinkhole;
		}

		private static readonly int SubmergeHash = Animator.StringToHash("IsSubmerged");

		private static readonly int SpeedHash = Animator.StringToHash("TransitionSpeed");

		[SerializeField]
		private AnimationCurve _submergeAnim;

		[SerializeField]
		private AnimationCurve _appearAnim;

		[SerializeField]
		private GameObject[] _hiddenObjects;

		private Transform _tr;

		private Vector3 _modelOffset;

		private Scp106SinkholeController _sinkhole;

		private Scp106StalkAbility _stalkAbility;

		private Scp106MovementModule _fpc;

		private const float DefaultEmergeAnimTime = 3.908f;

		private const float DefaultSubmergeAnimTime = 3.333f;
	}
}
