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

namespace PlayerRoles.FirstPersonControl.Thirdperson;

public class AnimatedCharacterModel : CharacterModel, IAnimatorLayerSource
{
	private enum FootstepLoudness
	{
		Civilian = 8,
		FoundationForces = 12,
		Chaos = 30,
		Scp = 35
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

	private float _lastMovedDeltaT;

	private IAnimatedModelSubcontroller[] _subcontrollers;

	private HashSet<int> _allParameterHashes;

	private float? _lastAppliedWalkLayerWeight;

	private const float SilentVelocityMultiplier = 0.7f;

	private const float SprintingLoudnessMultiplier = 2f;

	private const float MinimalFootstepSoundCooldown = 0.2f;

	private const float SpawnGroundedSuppression = 0.3f;

	private const float TimeSkipOnSetupSeconds = 10f;

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
	private FootstepLoudness _footstepLoudness;

	public ModelSharedSettings SharedSettings;

	public AudioClip RandomFootstep => this._footstepClips.RandomItem();

	public Vector3 HeadBobPosition { get; protected set; }

	public bool IsTracked
	{
		get
		{
			if (!base.Pooled)
			{
				if (!base.OwnerHub.isLocalPlayer)
				{
					return base.OwnerHub.IsLocallySpectated();
				}
				return true;
			}
			return false;
		}
	}

	public float LastMovedDeltaT
	{
		get
		{
			if (!base.HasOwner)
			{
				return Time.deltaTime;
			}
			return this._lastMovedDeltaT;
		}
	}

	internal Animator Animator { get; private set; }

	public AnimatorLayerManager LayerManager { get; private set; }

	internal AnimatorOverrideController AnimatorOverride { get; private set; }

	protected FirstPersonMovementModule FpcModule { get; private set; }

	protected PlayerRoleBase Role { get; private set; }

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
			float num = this.FpcModule.VelocityForState(PlayerMovementState.Sneaking, applyCrouch: false);
			float maxMovementSpeed = this.FpcModule.MaxMovementSpeed;
			if (maxMovementSpeed <= num && maxMovementSpeed > 0f)
			{
				return false;
			}
			num *= 0.7f;
			return this.FpcModule.Motor.Velocity.SqrMagnitudeIgnoreY() >= num * num;
		}
	}

	public virtual bool LandingFootstepPlayable => true;

	public float WalkCycle
	{
		get
		{
			float walkCycleRaw = this.WalkCycleRaw;
			if (!float.IsNaN(walkCycleRaw))
			{
				return walkCycleRaw - (float)(int)walkCycleRaw;
			}
			return 0f;
		}
	}

	public float WalkCycleRaw => this.Animator.GetCurrentAnimatorStateInfo(this.WalkLayerIndex).normalizedTime;

	public float WalkLayerWeight
	{
		get
		{
			float valueOrDefault = this._lastAppliedWalkLayerWeight.GetValueOrDefault();
			if (!this._lastAppliedWalkLayerWeight.HasValue)
			{
				valueOrDefault = this.LayerManager.GetLayerWeight(this._walkLayer);
				this._lastAppliedWalkLayerWeight = valueOrDefault;
			}
			return this._lastAppliedWalkLayerWeight.Value;
		}
		set
		{
			float num = Mathf.Clamp01(value);
			this.LayerManager.SetLayerWeight(this._walkLayer, num);
			this._lastAppliedWalkLayerWeight = num;
		}
	}

	public ReadOnlySpan<IAnimatedModelSubcontroller> AllSubcontrollers => new ReadOnlySpan<IAnimatedModelSubcontroller>(this._subcontrollers);

	public float CurrentStafe => this.Animator.GetFloat(AnimatedCharacterModel.HashStrafe);

	public float CurrentForward => this.Animator.GetFloat(AnimatedCharacterModel.HashForward);

	public float TargetStrafe { get; private set; }

	public float TargetForward { get; private set; }

	public RuntimeAnimatorController AnimLayersSource => base.GetComponentInChildren<Animator>().runtimeAnimatorController;

	private int WalkLayerIndex => this.LayerManager.GetLayerIndex(this._walkLayer);

	public event Action<PooledAudioSource> OnFootstepAudioSpawned;

	protected override void Awake()
	{
		base.Awake();
		this.Animator = this.SetupAnimator();
		this.LayerManager = this.Animator.GetComponent<AnimatorLayerManager>();
		this._allParameterHashes = new HashSet<int>(this.Animator.parameters.Select((AnimatorControllerParameter x) => x.nameHash));
		this._subcontrollers = base.GetComponents<IAnimatedModelSubcontroller>();
		for (int num = 0; num < this._subcontrollers.Length; num++)
		{
			this._subcontrollers[num].Init(this, num);
		}
	}

	protected virtual void Update()
	{
		if (!base.Pooled)
		{
			float dampTime = (base.OwnerHub.isLocalPlayer ? this._firstpersonDampTime : this._thirdpersonDampTime);
			float num = this.WalkCycleRaw;
			if (float.IsNaN(num))
			{
				num = 0f;
			}
			Vector2 movementDirection;
			float normalizedVelocity;
			if (!this.FpcModule.IsGrounded)
			{
				movementDirection = Vector2.zero;
				normalizedVelocity = 0f;
			}
			else
			{
				Vector3 vector = base.CachedTransform.InverseTransformDirection(this.FpcModule.Motor.Velocity);
				Vector2 vector2 = new Vector2(vector.x, vector.z);
				float magnitude = vector2.magnitude;
				movementDirection = ((magnitude <= float.Epsilon) ? Vector2.zero : (vector2 / magnitude));
				float walkSpeed = this.FpcModule.WalkSpeed;
				normalizedVelocity = ((walkSpeed == 0f) ? 1f : (magnitude / walkSpeed));
			}
			this.UpdateHeadBob(num);
			this.UpdateFootsteps(num);
			this.UpdateAnimatorParameters(movementDirection, normalizedVelocity, dampTime);
			if (this.HasParameter(AnimatedCharacterModel.HashGrounded))
			{
				bool value = this.FpcModule.Noclip.IsActive || this.Role.ActiveTime < 0.3f || this.FpcModule.IsGrounded;
				this.Animator.SetBool(AnimatedCharacterModel.HashGrounded, value);
			}
		}
	}

	public override void OnPlayerMove()
	{
		float a = (float)this._lastMovedSw.Elapsed.TotalSeconds;
		this._lastMovedSw.Restart();
		this._lastMovedDeltaT = Mathf.Min(a, Time.maximumDeltaTime);
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
		return AudioSourcePoolManager.PlayOnTransform(this.RandomFootstep, base.transform, dis, vol, FalloffType.Footstep);
	}

	private void UpdateHeadBob(float time)
	{
	}

	private void UpdateFootsteps(float time)
	{
		time -= (float)(int)time;
		int num = this._footstepTimes.Length;
		if (this._lastFootstep < num)
		{
			if (!(time < this._footstepTimes[this._lastFootstep]))
			{
				this._lastFootstep++;
				if (this.FootstepPlayable)
				{
					this.PlayFootstep();
				}
			}
		}
		else if (num > 0 && time < this._footstepTimes[0])
		{
			this._lastFootstep = 0;
		}
	}

	private void OnGrounded()
	{
		if (!(base.OwnerHub.roleManager.CurrentRole.ActiveTime < 0.3f) && this.LandingFootstepPlayable)
		{
			this.PlayFootstep();
			this._lastTouchdownSw.Restart();
			if (this.IsTracked)
			{
				this.SharedSettings.PlayLandingAnimation();
			}
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
			if (playerEffectsController.AllEffects[i].IsEnabled && playerEffectsController.AllEffects[i] is IFootstepEffect footstepEffect)
			{
				float num3 = footstepEffect.ProcessFootstepOverrides(footstepLoudnessDistance);
				if (num3 >= 0f)
				{
					flag = false;
				}
				num = Mathf.Min(num, num3);
			}
		}
		if (!flag || num >= 0f)
		{
			AnimatedCharacterModel.OnFootstepPlayed?.Invoke(this, footstepLoudnessDistance);
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
			if (subcontrollers[i] is T val)
			{
				subcontroller = val;
				return true;
			}
		}
		subcontroller = null;
		return false;
	}

	public virtual void UpdateAnimatorParameters(Vector2 movementDirection, float normalizedVelocity, float dampTime)
	{
		float value = this._walkVelocityScale.Evaluate(normalizedVelocity);
		movementDirection *= normalizedVelocity;
		this.TargetStrafe = movementDirection.x;
		this.TargetForward = movementDirection.y;
		if (this._forceUpdate)
		{
			this.Animator.SetFloat(AnimatedCharacterModel.HashForward, this.TargetForward);
			this.Animator.SetFloat(AnimatedCharacterModel.HashStrafe, this.TargetStrafe);
			this.Animator.SetFloat(AnimatedCharacterModel.HashSpeed, value);
		}
		else
		{
			this.Animator.SetFloat(AnimatedCharacterModel.HashForward, this.TargetForward, dampTime, Time.deltaTime);
			this.Animator.SetFloat(AnimatedCharacterModel.HashStrafe, this.TargetStrafe, dampTime, Time.deltaTime);
			this.Animator.SetFloat(AnimatedCharacterModel.HashSpeed, value, dampTime, Time.deltaTime);
		}
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
		fpcModule.OnGrounded = (Action)Delegate.Remove(fpcModule.OnGrounded, new Action(OnGrounded));
	}

	public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		base.Setup(owner, role, localPos, localRot);
		this.Role = role as PlayerRoleBase;
		this.FpcModule = role.FpcModule;
		FirstPersonMovementModule fpcModule = this.FpcModule;
		fpcModule.OnGrounded = (Action)Delegate.Combine(fpcModule.OnGrounded, new Action(OnGrounded));
		this.Animator.Rebind();
		this.Animator.Update(10f);
		HitboxIdentity[] hitboxes = base.Hitboxes;
		foreach (HitboxIdentity hitboxIdentity in hitboxes)
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

	public override void SetAsOwnerless()
	{
		base.SetAsOwnerless();
		this.Awake();
	}
}
