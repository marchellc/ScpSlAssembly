using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AnimatorLayerManagement;
using AudioPooling;
using CustomPlayerEffects;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson
{
	public class AnimatedCharacterModel : CharacterModel, IAnimatorLayerSource
	{
		public event Action<PooledAudioSource> OnFootstepAudioSpawned;

		public AudioClip RandomFootstep
		{
			get
			{
				return this._footstepClips.RandomItem<AudioClip>();
			}
		}

		public Vector3 HeadBobPosition { get; protected set; }

		public bool IsTracked
		{
			get
			{
				return !base.Pooled && (base.OwnerHub.isLocalPlayer || base.OwnerHub.IsLocallySpectated());
			}
		}

		public float LastMovedDeltaT { get; private set; }

		internal Animator Animator { get; private set; }

		public AnimatorLayerManager LayerManager { get; private set; }

		internal AnimatorOverrideController AnimatorOverride { get; private set; }

		private protected FirstPersonMovementModule FpcModule { protected get; private set; }

		private protected PlayerRoleBase Role { protected get; private set; }

		public virtual float FootstepLoudnessDistance
		{
			get
			{
				float num = (float)this._footstepLoudness;
				if (this.FpcModule.CurrentMovementState == PlayerMovementState.Sprinting)
				{
					num *= 2f;
				}
				return num;
			}
		}

		public virtual bool FootstepPlayable
		{
			get
			{
				if (!this.FpcModule.IsGrounded || !this.FpcModule.Motor.MovementDetected)
				{
					return false;
				}
				float num = this.FpcModule.VelocityForState(PlayerMovementState.Sneaking, false);
				float maxMovementSpeed = this.FpcModule.MaxMovementSpeed;
				if (maxMovementSpeed <= num && maxMovementSpeed > 0f)
				{
					return false;
				}
				num *= 0.7f;
				return this.FpcModule.Motor.Velocity.SqrMagnitudeIgnoreY() >= num * num;
			}
		}

		public virtual bool LandingFootstepPlayable
		{
			get
			{
				return true;
			}
		}

		public float WalkCycle
		{
			get
			{
				float walkCycleRaw = this.WalkCycleRaw;
				if (!float.IsNaN(walkCycleRaw))
				{
					return walkCycleRaw - (float)((int)walkCycleRaw);
				}
				return 0f;
			}
		}

		public float WalkCycleRaw
		{
			get
			{
				return this.Animator.GetCurrentAnimatorStateInfo(this.WalkLayerIndex).normalizedTime;
			}
		}

		public float WalkLayerWeight
		{
			get
			{
				float num = this._lastAppliedWalkLayerWeight.GetValueOrDefault();
				if (this._lastAppliedWalkLayerWeight == null)
				{
					num = this.LayerManager.GetLayerWeight(this._walkLayer);
					this._lastAppliedWalkLayerWeight = new float?(num);
				}
				return this._lastAppliedWalkLayerWeight.Value;
			}
			set
			{
				float num = Mathf.Clamp01(value);
				this.LayerManager.SetLayerWeight(this._walkLayer, num);
				this._lastAppliedWalkLayerWeight = new float?(num);
			}
		}

		public ReadOnlySpan<IAnimatedModelSubcontroller> AllSubcontrollers
		{
			get
			{
				return new ReadOnlySpan<IAnimatedModelSubcontroller>(this._subcontrollers);
			}
		}

		public float CurrentStafe
		{
			get
			{
				return this.Animator.GetFloat(AnimatedCharacterModel.HashStrafe);
			}
		}

		public float CurrentForward
		{
			get
			{
				return this.Animator.GetFloat(AnimatedCharacterModel.HashForward);
			}
		}

		public float TargetStrafe { get; private set; }

		public float TargetForward { get; private set; }

		public RuntimeAnimatorController AnimLayersSource
		{
			get
			{
				return base.GetComponentInChildren<Animator>().runtimeAnimatorController;
			}
		}

		private int WalkLayerIndex
		{
			get
			{
				return this.LayerManager.GetLayerIndex(this._walkLayer);
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this.Animator = this.SetupAnimator();
			this.LayerManager = this.Animator.GetComponent<AnimatorLayerManager>();
			this._allParameterHashes = new HashSet<int>(this.Animator.parameters.Select((AnimatorControllerParameter x) => x.nameHash));
			this._subcontrollers = base.GetComponents<IAnimatedModelSubcontroller>();
			for (int i = 0; i < this._subcontrollers.Length; i++)
			{
				this._subcontrollers[i].Init(this, i);
			}
		}

		protected virtual void Update()
		{
			if (base.Pooled)
			{
				return;
			}
			float num = (base.OwnerHub.isLocalPlayer ? this._firstpersonDampTime : this._thirdpersonDampTime);
			float num2 = this.WalkCycleRaw;
			if (float.IsNaN(num2))
			{
				num2 = 0f;
			}
			Vector2 vector;
			float num3;
			if (!this.FpcModule.IsGrounded)
			{
				vector = Vector2.zero;
				num3 = 0f;
			}
			else
			{
				Vector3 vector2 = base.CachedTransform.InverseTransformDirection(this.FpcModule.Motor.Velocity);
				Vector2 vector3 = new Vector2(vector2.x, vector2.z);
				float magnitude = vector3.magnitude;
				vector = ((magnitude <= float.Epsilon) ? Vector2.zero : (vector3 / magnitude));
				float walkSpeed = this.FpcModule.WalkSpeed;
				num3 = ((walkSpeed == 0f) ? 1f : (magnitude / walkSpeed));
			}
			this.UpdateHeadBob(num2);
			this.UpdateFootsteps(num2);
			this.UpdateAnimatorParameters(vector, num3, num);
			if (this.HasParameter(AnimatedCharacterModel.HashGrounded))
			{
				bool flag = this.FpcModule.Noclip.IsActive || this.Role.ActiveTime < 0.3f || this.FpcModule.IsGrounded;
				this.Animator.SetBool(AnimatedCharacterModel.HashGrounded, flag);
			}
		}

		public override void OnPlayerMove()
		{
			float num = (float)this._lastMovedSw.Elapsed.TotalSeconds;
			this._lastMovedSw.Restart();
			this.LastMovedDeltaT = Mathf.Min(num, Time.maximumDeltaTime);
			base.OnPlayerMove();
		}

		protected virtual Animator SetupAnimator()
		{
			Animator componentInChildren = base.GetComponentInChildren<Animator>();
			this.AnimatorOverride = new AnimatorOverrideController(componentInChildren.runtimeAnimatorController);
			componentInChildren.runtimeAnimatorController = this.AnimatorOverride;
			return componentInChildren;
		}

		protected virtual PooledAudioSource PlayFootstepAudioClip(AudioClip clip, float dis, float vol)
		{
			return AudioSourcePoolManager.PlayOnTransform(this.RandomFootstep, base.transform, dis, vol, FalloffType.Footstep, MixerChannel.DefaultSfx, 1f);
		}

		private void UpdateHeadBob(float time)
		{
		}

		private void UpdateFootsteps(float time)
		{
			time -= (float)((int)time);
			int num = this._footstepTimes.Length;
			if (this._lastFootstep >= num)
			{
				if (num > 0 && time < this._footstepTimes[0])
				{
					this._lastFootstep = 0;
				}
				return;
			}
			if (time < this._footstepTimes[this._lastFootstep])
			{
				return;
			}
			this._lastFootstep++;
			if (!this.FootstepPlayable)
			{
				return;
			}
			this.PlayFootstep();
		}

		private void OnGrounded()
		{
			if (base.OwnerHub.roleManager.CurrentRole.ActiveTime < 0.3f)
			{
				return;
			}
			if (!this.LandingFootstepPlayable)
			{
				return;
			}
			this.PlayFootstep();
			this._lastTouchdownSw.Restart();
			if (this.IsTracked)
			{
				this.SharedSettings.PlayLandingAnimation();
			}
		}

		private void PlayFootstep()
		{
			float footstepLoudnessDistance = this.FootstepLoudnessDistance;
			float num = 1f;
			bool flag = true;
			PlayerEffectsController playerEffectsController = base.OwnerHub.playerEffectsController;
			int num2 = playerEffectsController.AllEffects.Length;
			for (int i = 0; i < num2; i++)
			{
				if (playerEffectsController.AllEffects[i].IsEnabled)
				{
					IFootstepEffect footstepEffect = playerEffectsController.AllEffects[i] as IFootstepEffect;
					if (footstepEffect != null)
					{
						float num3 = footstepEffect.ProcessFootstepOverrides(footstepLoudnessDistance);
						if (num3 >= 0f)
						{
							flag = false;
						}
						num = Mathf.Min(num, num3);
					}
				}
			}
			if (!flag || num >= 0f)
			{
				Action<AnimatedCharacterModel, float> onFootstepPlayed = AnimatedCharacterModel.OnFootstepPlayed;
				if (onFootstepPlayed == null)
				{
					return;
				}
				onFootstepPlayed(this, footstepLoudnessDistance);
			}
		}

		public bool HasParameter(int hash)
		{
			return this._allParameterHashes.Contains(hash);
		}

		public bool TryGetSubcontroller<T>(out T subcontroller) where T : class
		{
			IAnimatedModelSubcontroller[] subcontrollers = this._subcontrollers;
			for (int i = 0; i < subcontrollers.Length; i++)
			{
				T t = subcontrollers[i] as T;
				if (t != null)
				{
					subcontroller = t;
					return true;
				}
			}
			subcontroller = default(T);
			return false;
		}

		public virtual void UpdateAnimatorParameters(Vector2 movementDirection, float normalizedVelocity, float dampTime)
		{
			float num = this._walkVelocityScale.Evaluate(normalizedVelocity);
			movementDirection *= normalizedVelocity;
			this.TargetStrafe = movementDirection.x;
			this.TargetForward = movementDirection.y;
			if (this._forceUpdate)
			{
				this.Animator.SetFloat(AnimatedCharacterModel.HashForward, this.TargetForward);
				this.Animator.SetFloat(AnimatedCharacterModel.HashStrafe, this.TargetStrafe);
				this.Animator.SetFloat(AnimatedCharacterModel.HashSpeed, num);
				return;
			}
			this.Animator.SetFloat(AnimatedCharacterModel.HashForward, this.TargetForward, dampTime, Time.deltaTime);
			this.Animator.SetFloat(AnimatedCharacterModel.HashStrafe, this.TargetStrafe, dampTime, Time.deltaTime);
			this.Animator.SetFloat(AnimatedCharacterModel.HashSpeed, num, dampTime, Time.deltaTime);
		}

		public virtual void ForceUpdate()
		{
			this._forceUpdate = true;
			this.Update();
			this._forceUpdate = false;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			FirstPersonMovementModule fpcModule = this.FpcModule;
			fpcModule.OnGrounded = (Action)Delegate.Remove(fpcModule.OnGrounded, new Action(this.OnGrounded));
		}

		public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
		{
			base.Setup(owner, role, localPos, localRot);
			this.Role = role as PlayerRoleBase;
			this.FpcModule = role.FpcModule;
			FirstPersonMovementModule fpcModule = this.FpcModule;
			fpcModule.OnGrounded = (Action)Delegate.Combine(fpcModule.OnGrounded, new Action(this.OnGrounded));
			this.Animator.Rebind();
			foreach (HitboxIdentity hitboxIdentity in this.Hitboxes)
			{
				HitboxIdentity.Instances.Add(hitboxIdentity);
				hitboxIdentity.SetColliders(!base.OwnerHub.isLocalPlayer);
			}
			IAnimatedModelSubcontroller[] subcontrollers = this._subcontrollers;
			for (int i = 0; i < subcontrollers.Length; i++)
			{
				subcontrollers[i].OnReassigned();
			}
		}

		public override void OnTreadmillInitialized()
		{
			base.OnTreadmillInitialized();
			this.Awake();
		}

		public static Action<AnimatedCharacterModel, float> OnFootstepPlayed;

		private static readonly int HashForward = Animator.StringToHash("Forward");

		private static readonly int HashGrounded = Animator.StringToHash("Grounded");

		private static readonly int HashStrafe = Animator.StringToHash("Strafe");

		private static readonly int HashSpeed = Animator.StringToHash("Speed");

		private readonly Stopwatch _lastTouchdownSw = Stopwatch.StartNew();

		private readonly Stopwatch _lastMovedSw = Stopwatch.StartNew();

		private int _lastFootstep;

		private bool _forceUpdate;

		private IAnimatedModelSubcontroller[] _subcontrollers;

		private HashSet<int> _allParameterHashes;

		private float? _lastAppliedWalkLayerWeight;

		private const float SilentVelocityMultiplier = 0.7f;

		private const float SprintingLoudnessMultiplier = 2f;

		private const float MinimalFootstepSoundCooldown = 0.2f;

		private const float SpawnGroundedSuppression = 0.3f;

		[Header("Animation settings")]
		[SerializeField]
		private float _firstpersonDampTime;

		[SerializeField]
		private float _thirdpersonDampTime;

		[SerializeField]
		private LayerRefId _walkLayer;

		[SerializeField]
		private AnimationCurve _walkVelocityScale;

		[Header("Footsteps")]
		[SerializeField]
		private AudioClip[] _footstepClips;

		[Range(0f, 1f)]
		[SerializeField]
		private float[] _footstepTimes;

		[SerializeField]
		private AnimatedCharacterModel.FootstepLoudness _footstepLoudness;

		public ModelSharedSettings SharedSettings;

		private enum FootstepLoudness
		{
			Civilian = 8,
			FoundationForces = 12,
			Chaos = 30,
			Scp = 35
		}
	}
}
