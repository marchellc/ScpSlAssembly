using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;

public class IdlePoseRetainerSubcontroller : SubcontrollerBehaviour, IRotationRetainer
{
	private enum TurnStatus
	{
		Idle,
		TurningRight,
		TurningLeft
	}

	private static readonly int TurnTriggerHash = Animator.StringToHash("StartTurn");

	private static readonly int TurnBlendHash = Animator.StringToHash("TurnDirection");

	private static readonly AnimationCurve TurningCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.114f, 4.725f, 123.39f, 123.39f), new Keyframe(0.4377f, 65.381f, 65.75f, 65.75f), new Keyframe(0.557f, 68.14f, -2.257f, -2.257f), new Keyframe(0.7f, 66f));

	private const float TurnAngle = 66f;

	private const float FullTurnTime = 0.7f;

	private const float FastTurnTime = 0.45f;

	private const float TurnAnimCooldown = 0.3f;

	private const float UncomfortableAngle = 35f;

	private const float UncomfortableTurnDelay = 2.5f;

	private const float MaxAngularDesync = 95f;

	private const float LayerAdjustSpeed = 5f;

	private const float FullLockThreshold = 0.12f;

	private const float UnlockThreshold = 0.3f;

	private float _uncomfortableElapsed;

	private float _startTurningRotation;

	private float _targetTurningRotation;

	private float _turningElapsed;

	private float _turningAnimCooldown;

	private TurnStatus _turnStatus;

	private float _prevAngleDelta;

	private Quaternion _relativeRotation;

	private byte _lastWaypointId;

	private Transform _lastOwnerRoot;

	public float AngleDelta { get; private set; }

	public float AngleAbsDiff { get; private set; }

	public float RetentionWeight => 1f - base.Model.WalkLayerWeight;

	public bool IsTurning => this._turnStatus != TurnStatus.Idle;

	private float OwnerRotation => this._lastOwnerRoot.eulerAngles.y;

	private float ModelRotation
	{
		get
		{
			return WaypointBase.GetWorldRotation(this._lastWaypointId, this._relativeRotation).eulerAngles.y;
		}
		set
		{
			Quaternion quaternion = Quaternion.Euler(Vector3.up * value);
			WaypointBase.GetRelativeRotation(base.ModelTr.position, quaternion, out this._lastWaypointId, out this._relativeRotation);
			base.ModelTr.rotation = quaternion;
		}
	}

	public override void OnReassigned()
	{
		base.OnReassigned();
		this._lastOwnerRoot = base.OwnerHub.transform;
	}

	public override void Init(AnimatedCharacterModel model, int index)
	{
		base.Init(model, index);
		model.OnPlayerMoved += OnPlayerMoved;
	}

	private void OnPlayerMoved()
	{
		float walkLayerWeight = base.Model.WalkLayerWeight;
		float target = Mathf.InverseLerp(0.12f, 0.3f, new Vector2(base.Model.TargetForward, base.Model.TargetStrafe).magnitude);
		walkLayerWeight = Mathf.MoveTowards(walkLayerWeight, target, base.Model.LastMovedDeltaT * 5f);
		this.ModelRotation = Mathf.LerpAngle(this.ModelRotation, this.OwnerRotation, walkLayerWeight);
		base.Model.WalkLayerWeight = walkLayerWeight;
	}

	private void Update()
	{
		if (base.HasOwner)
		{
			this.CalculateDeltas();
			this.ClampOwner();
			if (!this.IsTurning || this._turningElapsed > 0.45f)
			{
				this.UpdateComfort();
			}
			if (this.IsTurning)
			{
				this.UpdateTurning();
			}
		}
	}

	private void CalculateDeltas()
	{
		this._prevAngleDelta = this.AngleDelta;
		this.AngleDelta = Mathf.DeltaAngle(this.ModelRotation, this.OwnerRotation);
		this.AngleAbsDiff = Mathf.Abs(this.AngleDelta);
	}

	private void ClampOwner()
	{
		float num = this.AngleAbsDiff - 95f;
		if (!(num <= 0f))
		{
			if (this.AngleDelta > 0f)
			{
				this.ModelRotation += num;
			}
			else
			{
				this.ModelRotation -= num;
			}
			this.CalculateDeltas();
			this.StartTurning();
		}
	}

	private void UpdateTurning()
	{
		if (base.Model.WalkLayerWeight > 0f)
		{
			this._turnStatus = TurnStatus.Idle;
			return;
		}
		float num = Mathf.DeltaAngle(this._startTurningRotation, this._targetTurningRotation);
		base.Animator.SetFloat(IdlePoseRetainerSubcontroller.TurnBlendHash, num / 66f);
		float num2 = this._startTurningRotation;
		float num3 = this._targetTurningRotation;
		if (this._turnStatus == TurnStatus.TurningLeft)
		{
			for (; num2 < num3; num2 += 360f)
			{
			}
		}
		else
		{
			for (; num2 > num3; num3 += 360f)
			{
			}
		}
		this._turningElapsed += Time.deltaTime;
		this._turningAnimCooldown -= Time.deltaTime;
		float t = IdlePoseRetainerSubcontroller.TurningCurve.Evaluate(this._turningElapsed) / 66f;
		this.ModelRotation = Mathf.LerpUnclamped(num2, num3, t);
		if (this._turningElapsed > 0.7f)
		{
			this._turnStatus = TurnStatus.Idle;
		}
	}

	private void UpdateComfort()
	{
		if (this.AngleAbsDiff < 35f || !this.AngleDelta.SameSign(this._prevAngleDelta))
		{
			this._uncomfortableElapsed = 0f;
			return;
		}
		if (!this.IsTurning)
		{
			this._uncomfortableElapsed += Time.deltaTime;
		}
		float num = Remap.Evaluate(35f, 66f, 2.5f, 0f, this.AngleAbsDiff);
		if (this._uncomfortableElapsed > num)
		{
			this._uncomfortableElapsed = 0f;
			this.StartTurning();
		}
	}

	private void StartTurning()
	{
		if (this._turnStatus == TurnStatus.Idle || this._turningAnimCooldown <= 0f)
		{
			this._turningAnimCooldown = 0.3f;
			base.Animator.SetTrigger(IdlePoseRetainerSubcontroller.TurnTriggerHash);
		}
		this._turnStatus = ((this.AngleDelta > 0f) ? TurnStatus.TurningRight : TurnStatus.TurningLeft);
		this._turningElapsed = 0f;
		this._startTurningRotation = this.ModelRotation;
		this._targetTurningRotation = this.OwnerRotation;
	}
}
