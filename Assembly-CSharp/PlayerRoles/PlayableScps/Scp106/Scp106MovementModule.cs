using System;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106MovementModule : FirstPersonMovementModule, IFpcCollisionModifier
	{
		private float MovementSpeed
		{
			get
			{
				return this.WalkSpeed;
			}
			set
			{
				this.SneakSpeed = value;
				this.WalkSpeed = value;
				this.SprintSpeed = value;
			}
		}

		public float CurSlowdown { get; private set; }

		public LayerMask DetectionMask
		{
			get
			{
				return Scp106MovementModule.PassableDetectionMask;
			}
		}

		public static float GetSlowdownFromCollider(Collider col)
		{
			DoorVariant doorVariant;
			if (!col.transform.TryGetComponentInParent(out doorVariant))
			{
				return (float)((col.gameObject.layer == 14 && col is BoxCollider) ? 1 : 0);
			}
			IScp106PassableDoor scp106PassableDoor = doorVariant as IScp106PassableDoor;
			if (scp106PassableDoor == null)
			{
				return 0f;
			}
			if (!scp106PassableDoor.IsScp106Passable)
			{
				return 0f;
			}
			float exactState = doorVariant.GetExactState();
			return Mathf.Clamp01(1f - exactState);
		}

		private void Awake()
		{
			this._slowndownSpeed = this.SneakSpeed;
			this._normalSpeed = this.WalkSpeed;
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
			Scp106SinkholeController.OnSubmergeStateChange += this.GrantSpeedBoost;
		}

		public void ProcessColliders(ArraySegment<Collider> detections)
		{
			this._slowndownTarget = (float)(this._sinkhole.TargetSubmerged ? 1 : 0);
			foreach (Collider collider in detections)
			{
				float slowdownFromCollider = Scp106MovementModule.GetSlowdownFromCollider(collider);
				collider.enabled = slowdownFromCollider == 0f;
				this._slowndownTarget = Mathf.Max(this._slowndownTarget, slowdownFromCollider);
			}
		}

		private void GrantSpeedBoost(Scp106Role scp106role, bool isSubmerged)
		{
			if (isSubmerged || base.Role != scp106role)
			{
				return;
			}
			this._remainingSpeedBoostDuration = scp106role.Sinkhole.TargetTransitionDuration + 5f;
		}

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
	}
}
