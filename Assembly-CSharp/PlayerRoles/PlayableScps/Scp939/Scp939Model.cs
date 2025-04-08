using System;
using System.Diagnostics;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps.Subroutines;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939Model : AnimatedCharacterModel
	{
		private bool Visible
		{
			get
			{
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetPovHub(out referenceHub))
				{
					return true;
				}
				if (!HitboxIdentity.IsEnemy(base.OwnerHub, referenceHub))
				{
					return true;
				}
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole == null)
				{
					return true;
				}
				Vector3 position = fpcRole.FpcModule.Position;
				if (Vector3.Distance(position, this._scp939.FpcModule.Position) < this._amnesiaVisibleRange)
				{
					return true;
				}
				AmnesiaVision amnesiaVision;
				if (!referenceHub.playerEffectsController.TryGetEffect<AmnesiaVision>(out amnesiaVision))
				{
					return true;
				}
				if (amnesiaVision.IsEnabled)
				{
					return false;
				}
				if (amnesiaVision.LastActive < 30f)
				{
					return true;
				}
				foreach (Scp939AmnesticCloudInstance scp939AmnesticCloudInstance in Scp939AmnesticCloudInstance.ActiveInstances)
				{
					if (scp939AmnesticCloudInstance.IsInArea(scp939AmnesticCloudInstance.SourcePosition, position))
					{
						return false;
					}
				}
				return true;
			}
		}

		public override bool FootstepPlayable
		{
			get
			{
				return base.FootstepPlayable && base.FpcModule.CurrentMovementState == PlayerMovementState.Sprinting;
			}
		}

		private void PlayClawAttack(AttackResult attackRes)
		{
			if (attackRes == AttackResult.None)
			{
				return;
			}
			this._animator.SetTrigger(Scp939Model.ClawHash);
		}

		private void ProcessLungeState(Scp939LungeState newState)
		{
			if (newState != Scp939LungeState.None)
			{
				if (newState == Scp939LungeState.Triggered)
				{
					this._lungeStopwatch.Restart();
				}
				this._isLunging = true;
				this._animator.SetInteger(Scp939Model.LungeStateHash, (int)newState);
				this._animator.SetTrigger(Scp939Model.LungeTriggerHash);
				return;
			}
			this._isLunging = false;
		}

		private void OnSpectatorTargetChanged()
		{
			this.ForceFade(1f);
		}

		private void UpdateFade()
		{
			this.ForceFade(this._fadeSpeed * Time.deltaTime);
		}

		private void ForceFade(float delta)
		{
			this.Fade += (this.Visible ? delta : (-delta));
			if (this.Fade > 0f || base.Role.IsLocalPlayer || NetworkServer.active)
			{
				return;
			}
			this._fadeoutStopwatch.Restart();
			base.FpcModule.Position = Vector3.up * -3000f;
		}

		protected override void Awake()
		{
			base.Awake();
			this._trModel = base.transform;
		}

		protected override void Update()
		{
			base.Update();
			base.Animator.SetBool(Scp939Model.GroundedHash, base.FpcModule.IsGrounded);
			base.Animator.SetBool(Scp939Model.AmnesticChargingHash, this._amnesticAbility.TargetState);
			float state = this._focusAbility.State;
			base.Animator.SetFloat(Scp939Model.FocusStateHash, state);
			base.Animator.SetLayerWeight(6, state);
			if (this._isLunging)
			{
				base.Animator.SetLayerWeight(4, 1f);
				return;
			}
			base.Animator.SetLayerWeight(4, this._focusOverrideAnim.Evaluate(state));
		}

		public void PlayDamagedEffect(int rand)
		{
			rand %= this._damagedVariants.Length;
			this._animator.SetFloat(Scp939Model.DamagedVariantHash, (float)rand);
			this._animator.SetTrigger(Scp939Model.DamagedTriggerHash);
			AudioSourcePoolManager.PlayOnTransform(this._damagedVariants[rand], base.transform, 19f, 1f, FalloffType.Exponential, MixerChannel.NoDucking, 1f);
		}

		public void PlayCloudRelease()
		{
			this._animator.SetTrigger(Scp939Model.AmnesticTriggerHash);
			AudioSourcePoolManager.PlayOnTransform(this._cloudPlaceSound, base.transform, 8f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		public override void UpdateAnimatorParameters(Vector2 movementDirection, float normalizedVelocity, float dampTime)
		{
			if (this._focusAbility.State > 0f)
			{
				float num = this._focusParamsCorrectionCurve.Evaluate(this._focusAbility.State);
				float num2 = Vector3.Dot(movementDirection.normalized, Vector2.up);
				normalizedVelocity *= Mathf.Lerp(1f, num, Mathf.Abs(num2));
			}
			base.UpdateAnimatorParameters(movementDirection, normalizedVelocity, dampTime);
		}

		private void LateUpdate()
		{
			float num = Time.deltaTime * this._tiltLerp;
			if (this._lungeAbility.State == Scp939LungeState.Triggered)
			{
				double totalSeconds = this._lungeStopwatch.Elapsed.TotalSeconds;
				float num2 = this._tiltOverTime.Evaluate((float)totalSeconds);
				this._curTilt = Mathf.Lerp(this._curTilt, num2, num);
			}
			else
			{
				this._curTilt = Mathf.Lerp(this._curTilt, 0f, num);
			}
			if (this._focusAbility.State == 0f)
			{
				if (!this._prevFocus)
				{
					return;
				}
				this._trModel.localRotation = Quaternion.identity;
				this._prevFocus = false;
				return;
			}
			else
			{
				if (!this._prevFocus)
				{
					this._prevFocus = true;
					return;
				}
				float num3;
				if (this._isLunging)
				{
					double totalSeconds2 = this._lungeStopwatch.Elapsed.TotalSeconds;
					num3 = 1f - (float)totalSeconds2 * 7.5f;
				}
				else
				{
					num3 = this._focusAbility.State * 3f;
				}
				Quaternion quaternion = Quaternion.Euler(0f, this._focusAbility.FrozenRotation, 0f);
				this._trModel.rotation = Quaternion.Slerp(this._trHub.rotation, quaternion, num3);
				this._trModel.Rotate(Vector3.right, this._curTilt, Space.Self);
				float num4 = Mathf.DeltaAngle(this._trHub.eulerAngles.y, this._trModel.eulerAngles.y);
				base.Animator.SetFloat(Scp939Model.FocusHeadDirHash, num4, 0.4f, Time.deltaTime);
				return;
			}
		}

		protected override Animator SetupAnimator()
		{
			return this._animator;
		}

		protected override PooledAudioSource PlayFootstepAudioClip(AudioClip clip, float dis, float vol)
		{
			PooledAudioSource pooledAudioSource = base.PlayFootstepAudioClip(clip, dis, vol);
			pooledAudioSource.Source.pitch = global::UnityEngine.Random.Range(this._footstepPitchRand.x, this._footstepPitchRand.y);
			return pooledAudioSource;
		}

		public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
		{
			base.Setup(owner, role, localPos, localRot);
			this._trHub = base.OwnerHub.transform;
			this._scp939 = base.OwnerHub.roleManager.CurrentRole as Scp939Role;
			this._scp939.SubroutineModule.TryGetSubroutine<Scp939ClawAbility>(out this._clawAbility);
			this._scp939.SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out this._focusAbility);
			this._scp939.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out this._lungeAbility);
			this._scp939.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out this._amnesticAbility);
			this._clawAbility.OnAttacked += this.PlayClawAttack;
			this._lungeAbility.OnStateChanged += this.ProcessLungeState;
			FirstPersonMovementModule.OnPositionUpdated += this.UpdateFade;
			SpectatorTargetTracker.OnTargetChanged += this.OnSpectatorTargetChanged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._clawAbility.OnAttacked -= this.PlayClawAttack;
			this._lungeAbility.OnStateChanged -= this.ProcessLungeState;
			this._curTilt = 0f;
			this._prevFocus = false;
			this._isLunging = false;
			FirstPersonMovementModule.OnPositionUpdated -= this.UpdateFade;
			SpectatorTargetTracker.OnTargetChanged -= this.OnSpectatorTargetChanged;
		}

		public bool IsInHiddenPosition
		{
			get
			{
				return base.FpcModule.Position == Vector3.up * -3000f;
			}
		}

		[SerializeField]
		private Animator _animator;

		[SerializeField]
		private AnimationCurve _focusOverrideAnim;

		[SerializeField]
		private AnimationCurve _tiltOverTime;

		[SerializeField]
		private AnimationCurve _focusParamsCorrectionCurve;

		[SerializeField]
		private AudioClip[] _damagedVariants;

		[SerializeField]
		private AudioClip _cloudPlaceSound;

		[SerializeField]
		private float _tiltLerp;

		[SerializeField]
		private float _fadeSpeed;

		[SerializeField]
		private Vector2 _footstepPitchRand;

		[SerializeField]
		private float _amnesiaVisibleRange;

		private Scp939Role _scp939;

		private Scp939ClawAbility _clawAbility;

		private Scp939FocusAbility _focusAbility;

		private Scp939LungeAbility _lungeAbility;

		private Scp939AmnesticCloudAbility _amnesticAbility;

		private Transform _trModel;

		private Transform _trHub;

		private bool _prevFocus;

		private bool _isLunging;

		private float _curTilt;

		private readonly Stopwatch _lungeStopwatch = new Stopwatch();

		private readonly Stopwatch _fadeoutStopwatch = Stopwatch.StartNew();

		private const int FocusOverrideLayer = 4;

		private const int FocusHeadDirLayer = 6;

		private const float FocusHeadFadeTime = 0.4f;

		private const float FocusRotateRate = 3f;

		private const float LungeRotateSpeed = 7.5f;

		private const float DamagedSoundRange = 19f;

		private const float CloudSoundRange = 8f;

		private const float HiddenHeight = -3000f;

		private const float FullVisCooldown = 30f;

		private static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

		private static readonly int ClawHash = Animator.StringToHash("Claw");

		private static readonly int FocusStateHash = Animator.StringToHash("Focus");

		private static readonly int FocusHeadDirHash = Animator.StringToHash("FocusDirection");

		private static readonly int LungeStateHash = Animator.StringToHash("LungeState");

		private static readonly int LungeTriggerHash = Animator.StringToHash("LungeTrigger");

		private static readonly int DamagedVariantHash = Animator.StringToHash("DamagedVariant");

		private static readonly int DamagedTriggerHash = Animator.StringToHash("DamagedTrigger");

		private static readonly int AmnesticChargingHash = Animator.StringToHash("AmnesticCharging");

		private static readonly int AmnesticTriggerHash = Animator.StringToHash("AmnesticCreated");
	}
}
