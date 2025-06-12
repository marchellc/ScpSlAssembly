using System;
using System.Diagnostics;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173CharacterModel : CharacterModel
{
	public delegate void ModelFrozen(Scp173Role target);

	[SerializeField]
	private float _lowestPitch;

	[SerializeField]
	private AudioSource[] _footstepSources;

	[SerializeField]
	private float _footstepOverallLoundess;

	[SerializeField]
	private float _footstepSwapSpeed;

	[SerializeField]
	private float _footstepEnableSpeed;

	[SerializeField]
	private float _footstepDisableSpeed;

	[SerializeField]
	private float _groundedSustainTime;

	[SerializeField]
	private float _footstepGroundedSustainMultiplier;

	private readonly Stopwatch _groundedSw = Stopwatch.StartNew();

	private int _sourcesCount;

	private float _stepSize;

	private bool _isFrozen;

	private float _currentVolume;

	private Quaternion _frozenRot;

	private Scp173Role _role;

	private Scp173MovementModule _fpc;

	private Scp173ObserversTracker _observers;

	public bool Frozen
	{
		get
		{
			return this._isFrozen;
		}
		set
		{
			if (value != this._isFrozen)
			{
				this._isFrozen = value;
				if (this._isFrozen)
				{
					this._frozenRot = base.transform.rotation;
					Scp173CharacterModel.OnFrozen?.Invoke(this._role);
				}
				else
				{
					base.transform.localRotation = Quaternion.identity;
				}
			}
		}
	}

	public static event ModelFrozen OnFrozen;

	private void LateUpdate()
	{
		if (base.HasOwner && ReferenceHub.TryGetLocalHub(out var hub))
		{
			this.Frozen = HitboxIdentity.IsEnemy(Team.SCPs, hub.GetTeam()) && this._observers.IsObservedBy(hub);
			this.UpdateFootsteps(!this.Frozen && this._fpc.Motor.Velocity != Vector3.zero, this._fpc.IsGrounded);
			if (this.Frozen)
			{
				base.transform.rotation = this._frozenRot;
			}
		}
	}

	private void UpdateFootsteps(bool isMoving, bool grounded)
	{
		float num = (isMoving ? this._footstepEnableSpeed : this._footstepDisableSpeed);
		if (grounded)
		{
			this._groundedSw.Restart();
		}
		else if (isMoving && this._groundedSw.Elapsed.TotalSeconds < (double)this._groundedSustainTime)
		{
			num *= this._footstepGroundedSustainMultiplier;
		}
		float num2 = Mathf.MoveTowards(this._currentVolume, (isMoving && grounded) ? 1 : 0, Time.deltaTime * num);
		float pitch = Mathf.Lerp(this._lowestPitch, 1f, num2);
		float num3 = Time.timeSinceLevelLoad * this._footstepSwapSpeed;
		this._currentVolume = num2;
		num2 *= this._footstepOverallLoundess;
		for (int i = 0; i < this._sourcesCount; i++)
		{
			AudioSource obj = this._footstepSources[i];
			float f = Mathf.Sin(num3 + MathF.PI * this._stepSize * (float)i);
			obj.pitch = pitch;
			obj.volume = num2 * Mathf.Abs(f);
		}
	}

	private void OnGrounded()
	{
		this._currentVolume = 1f;
	}

	public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		base.Setup(owner, role, localPos, localRot);
		this._role = base.OwnerHub.roleManager.CurrentRole as Scp173Role;
		this._fpc = this._role.FpcModule as Scp173MovementModule;
		Scp173MovementModule fpc = this._fpc;
		fpc.OnGrounded = (Action)Delegate.Combine(fpc.OnGrounded, new Action(OnGrounded));
		this._role.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observers);
		this._sourcesCount = this._footstepSources.Length;
		this._stepSize = 1f / (float)this._sourcesCount;
		for (int i = 0; i < this._sourcesCount; i++)
		{
			AudioSource obj = this._footstepSources[i];
			obj.volume = 0f;
			obj.PlayDelayed(obj.clip.length * this._stepSize * (float)i);
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		FirstPersonMovementModule fpcModule = this._role.FpcModule;
		fpcModule.OnGrounded = (Action)Delegate.Remove(fpcModule.OnGrounded, new Action(OnGrounded));
		for (int i = 0; i < this._sourcesCount; i++)
		{
			this._footstepSources[i].Stop();
		}
	}
}
