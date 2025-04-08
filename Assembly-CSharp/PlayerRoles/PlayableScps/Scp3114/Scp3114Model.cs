using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Model : AnimatedCharacterModel
	{
		public override bool FootstepPlayable
		{
			get
			{
				return base.FootstepPlayable && this.ScpRole.SkeletonIdle;
			}
		}

		public override bool LandingFootstepPlayable
		{
			get
			{
				return base.LandingFootstepPlayable && this.ScpRole.SkeletonIdle;
			}
		}

		private Scp3114Role ScpRole
		{
			get
			{
				return base.Role as Scp3114Role;
			}
		}

		public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
		{
			base.Setup(owner, role, localPos, localRot);
			this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114Strangle>(out this._strangle);
			this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114Dance>(out this._dance);
			this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114Slap>(out this._slap);
			this._slap.OnTriggered += this.OnSlapTriggered;
			this.ScpRole.CurIdentity.OnStatusChanged += this.OnIdentityChanged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._hadIdentity = false;
			this._slap.OnTriggered -= this.OnSlapTriggered;
			this.ScpRole.CurIdentity.OnStatusChanged -= this.OnIdentityChanged;
		}

		protected override void Update()
		{
			base.Update();
			if (base.Pooled)
			{
				return;
			}
			bool flag = this._strangle.SyncTarget != null;
			base.Animator.SetBool(Scp3114Model.HashStrangling, flag);
			base.Animator.SetFloat(Scp3114Model.HashDanceVariant, (float)this._dance.DanceVariant);
			base.Animator.SetBool(Scp3114Model.HashStealing, this.ScpRole.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Equipping);
			this.AdjustWeight(this._strangleLayer, flag);
			this.AdjustWeight(this._danceLayer, this._dance.IsDancing);
		}

		private void AdjustWeight(int layer, bool isActive)
		{
			float layerWeight = base.Animator.GetLayerWeight(layer);
			float num = Time.deltaTime * this._layerWeightAdjustSpeed;
			float num2 = (isActive ? (layerWeight + num) : (layerWeight - num));
			base.Animator.SetLayerWeight(layer, Mathf.Clamp01(num2));
		}

		private void OnIdentityChanged()
		{
			this._skeletonFormItemRoot.SetActive(this.ScpRole.SkeletonIdle);
			Scp3114Identity.DisguiseStatus status = this.ScpRole.CurIdentity.Status;
			if (status != Scp3114Identity.DisguiseStatus.None)
			{
				if (status == Scp3114Identity.DisguiseStatus.Active)
				{
					this._hadIdentity = true;
					return;
				}
			}
			else if (this._hadIdentity)
			{
				this._hadIdentity = false;
				this._revealParticles.Play(true);
				base.Animator.SetTrigger(Scp3114Model.HashReveal);
			}
		}

		private void OnSlapTriggered()
		{
			base.Animator.SetBool(Scp3114Model.HashSlapMirror, !base.Animator.GetBool(Scp3114Model.HashSlapMirror));
			base.Animator.SetTrigger(Scp3114Model.HashSlapTrigger);
		}

		private Scp3114Strangle _strangle;

		private Scp3114Slap _slap;

		private Scp3114Dance _dance;

		private bool _hadIdentity;

		[SerializeField]
		private int _strangleLayer;

		[SerializeField]
		private int _danceLayer;

		[SerializeField]
		private float _layerWeightAdjustSpeed;

		[SerializeField]
		private ParticleSystem _revealParticles;

		[SerializeField]
		private GameObject _skeletonFormItemRoot;

		private static readonly int HashStrangling = Animator.StringToHash("Strangling");

		private static readonly int HashDanceVariant = Animator.StringToHash("DanceVariant");

		private static readonly int HashSlapTrigger = Animator.StringToHash("SlapTrigger");

		private static readonly int HashSlapMirror = Animator.StringToHash("MirrorSlap");

		private static readonly int HashStealing = Animator.StringToHash("Stealing");

		private static readonly int HashReveal = Animator.StringToHash("Reveal");
	}
}
