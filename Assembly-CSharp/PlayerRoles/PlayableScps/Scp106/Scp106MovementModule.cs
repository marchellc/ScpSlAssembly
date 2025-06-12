using System;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class Scp106MovementModule : FirstPersonMovementModule, IFpcCollisionModifier
{
	[SerializeField]
	private float _stalkSpeed;

	private const float SubmergingLerp = 1.5f;

	private const int GlassLayer = 14;

	private const int DoorLayer = 27;

	private const float SlowdownTransitionSpeed = 5.5f;

	private const float SpeedBoostDuration = 5f;

	private const float SpeedBoostMultiplier = 1.2f;

	private float _slowndownTarget;

	private float _slowndownSpeed;

	private float _normalSpeed;

	private float _remainingSpeedBoostDuration;

	private Scp106SinkholeController _sinkhole;

	public static readonly int PassableDetectionMask = 134234112;

	private float MovementSpeed
	{
		get
		{
			return base.WalkSpeed;
		}
		set
		{
			base.SneakSpeed = value;
			base.WalkSpeed = value;
			base.SprintSpeed = value;
		}
	}

	public float CurSlowdown { get; private set; }

	public LayerMask DetectionMask => Scp106MovementModule.PassableDetectionMask;

	public static float GetSlowdownFromCollider(Collider col, out bool isPassable)
	{
		isPassable = false;
		if (col.transform.TryGetComponentInParent<DoorVariant>(out var comp))
		{
			if (!(comp is IScp106PassableDoor scp106PassableDoor))
			{
				return 0f;
			}
			if (!scp106PassableDoor.IsScp106Passable)
			{
				return 0f;
			}
			isPassable = true;
			float exactState = comp.GetExactState();
			return Mathf.Clamp01(1f - exactState);
		}
		if (col.gameObject.layer == 14 && col is BoxCollider)
		{
			isPassable = true;
			return 1f;
		}
		return 0f;
	}

	private void Awake()
	{
		this._slowndownSpeed = base.SneakSpeed;
		this._normalSpeed = base.WalkSpeed;
		this.MovementSpeed = this._normalSpeed;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		this.CurSlowdown = Mathf.MoveTowards(this.CurSlowdown, this._slowndownTarget, deltaTime * 5.5f);
		float num;
		if (this._sinkhole.IsDuringAnimation)
		{
			num = Mathf.Lerp(this.MovementSpeed, 0f, 1.5f * deltaTime);
		}
		else
		{
			float num2 = Mathf.Lerp(this._normalSpeed, this._slowndownSpeed, this.CurSlowdown);
			num = (this._sinkhole.TargetSubmerged ? this._stalkSpeed : num2);
		}
		if (this._remainingSpeedBoostDuration > 0f)
		{
			this._remainingSpeedBoostDuration -= deltaTime;
			num *= 1.2f;
		}
		this.MovementSpeed = num;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		Scp106Role scp106Role = base.Role as Scp106Role;
		FpcCollisionProcessor.AddModifier(this, scp106Role);
		this._sinkhole = scp106Role.Sinkhole;
		Scp106SinkholeController.OnSubmergeStateChange += GrantSpeedBoost;
	}

	public void ProcessColliders(ArraySegment<Collider> detections)
	{
		this._slowndownTarget = (this._sinkhole.TargetSubmerged ? 1 : 0);
		foreach (Collider item in detections)
		{
			bool isPassable;
			float slowdownFromCollider = Scp106MovementModule.GetSlowdownFromCollider(item, out isPassable);
			item.enabled = slowdownFromCollider == 0f;
			this._slowndownTarget = Mathf.Max(this._slowndownTarget, slowdownFromCollider);
		}
	}

	private void GrantSpeedBoost(Scp106Role scp106role, bool isSubmerged)
	{
		if (!isSubmerged && !(base.Role != scp106role))
		{
			this._remainingSpeedBoostDuration = scp106role.Sinkhole.TargetTransitionDuration + 5f;
		}
	}
}
