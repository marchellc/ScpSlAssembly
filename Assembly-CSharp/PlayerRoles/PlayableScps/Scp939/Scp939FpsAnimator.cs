using System;
using AudioPooling;
using PlayerRoles.PlayableScps.HUDs;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939FpsAnimator : ScpViewmodelBase
	{
		public override float CamFOV
		{
			get
			{
				return 36f;
			}
		}

		protected override void Start()
		{
			base.Start();
			Scp939Role scp939Role = base.Role as Scp939Role;
			scp939Role.SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out this._focusAbility);
			scp939Role.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out this._lungeAbility);
			scp939Role.SubroutineModule.TryGetSubroutine<Scp939ClawAbility>(out this._clawAbility);
			scp939Role.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out this._cloudAbility);
			this._fpc = scp939Role.FpcModule as Scp939MovementModule;
			this._model = this._fpc.CharacterModelInstance as Scp939Model;
			this._lungeAbility.OnStateChanged += this.OnLungeStateChanged;
			this._clawAbility.OnTriggered += this.OnAttackTriggered;
			this.OnLungeStateChanged(this._lungeAbility.State);
			if (base.Owner.isLocalPlayer)
			{
				return;
			}
			this.UpdateAnimations();
			this.SkipAnimations(2.5f, 3);
		}

		private void OnLungeStateChanged(Scp939LungeState state)
		{
			if (state == Scp939LungeState.None)
			{
				return;
			}
			base.Anim.SetInteger(Scp939FpsAnimator.LungeStateHash, (int)state);
			base.Anim.SetTrigger(Scp939FpsAnimator.LungeTriggerHash);
		}

		private void OnAttackTriggered()
		{
			base.Anim.SetTrigger(Scp939FpsAnimator.ClawAttackHash);
			AudioSourcePoolManager.Play2D(this._attackSound, 1f, MixerChannel.DefaultSfx, global::UnityEngine.Random.Range(this._pitchRandomization.x, this._pitchRandomization.y));
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (this._lungeAbility != null)
			{
				this._lungeAbility.OnStateChanged -= this.OnLungeStateChanged;
			}
			if (this._clawAbility != null)
			{
				this._clawAbility.OnTriggered -= this.OnAttackTriggered;
			}
		}

		private void SetCloudLayer(float weight, bool charging)
		{
			base.Anim.SetBool(Scp939FpsAnimator.CloudHash, charging);
			float num = Time.deltaTime * 1.5f;
			weight += (charging ? num : (-num));
			base.Anim.SetLayerWeight(4, Mathf.Clamp(weight, 0f, 2.5f));
		}

		protected override void UpdateAnimations()
		{
			this.SetCloudLayer(base.Anim.GetLayerWeight(4), this._cloudAbility.TargetState);
			float num = (this._fpc.IsGrounded ? this._fpc.Motor.Velocity.MagnitudeIgnoreY() : 0f);
			base.Anim.SetFloat(Scp939FpsAnimator.WalkCycleHash, this._model.WalkCycle);
			base.Anim.SetFloat(Scp939FpsAnimator.WalkBlendHash, num, this._dampTimeBlend, Time.deltaTime);
			base.Anim.SetBool(Scp939FpsAnimator.JumpingHash, !this._fpc.IsGrounded);
			base.Anim.SetBool(Scp939FpsAnimator.FocusActiveHash, this._focusAbility.TargetState || this._focusAbility.State > 0.4f);
			float layerWeight = base.Anim.GetLayerWeight(2);
			bool flag = this._clawAbility.Cooldown.IsReady && this._focusAbility.State == 0f;
			float num2 = Mathf.Max(0f, base.Role.ActiveTime - 0.4f);
			float num3 = Mathf.Min((float)(flag ? 1 : 0), num2);
			base.Anim.SetLayerWeight(2, Mathf.MoveTowards(layerWeight, num3, Time.deltaTime * 4.5f));
		}

		private const int JumpLayer = 2;

		private const float MinFocusStateToDisplay = 0.4f;

		private const float JumpLayerAdjustmentSpeed = 4.5f;

		private const float JumpOverLifetime = 0.4f;

		private const int CloudLayer = 4;

		private const float CloudTransitionSpeed = 1.5f;

		private const float CloudMaxWeight = 2.5f;

		private static readonly int WalkCycleHash = Animator.StringToHash("WalkCycle");

		private static readonly int WalkBlendHash = Animator.StringToHash("WalkBlend");

		private static readonly int ClawAttackHash = Animator.StringToHash("ClawAttack");

		private static readonly int FocusActiveHash = Animator.StringToHash("Focus");

		private static readonly int JumpingHash = Animator.StringToHash("IsJumping");

		private static readonly int LungeStateHash = Animator.StringToHash("LungeState");

		private static readonly int LungeTriggerHash = Animator.StringToHash("LungeTrigger");

		private static readonly int CloudHash = Animator.StringToHash("ChargingCloud");

		[SerializeField]
		private float _dampTimeBlend;

		[SerializeField]
		private AudioClip _attackSound;

		[SerializeField]
		private Vector2 _pitchRandomization;

		private Scp939Model _model;

		private Scp939MovementModule _fpc;

		private Scp939FocusAbility _focusAbility;

		private Scp939LungeAbility _lungeAbility;

		private Scp939ClawAbility _clawAbility;

		private Scp939AmnesticCloudAbility _cloudAbility;
	}
}
