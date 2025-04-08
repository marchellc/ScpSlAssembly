using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp096
{
	public class Scp096MovementModule : FirstPersonMovementModule
	{
		private float MovementSpeed
		{
			set
			{
				this.SneakSpeed = value;
				this.WalkSpeed = value;
				this.SprintSpeed = value;
			}
		}

		public override bool LockMovement
		{
			get
			{
				if (!base.Role.IsLocalPlayer)
				{
					return false;
				}
				Scp096AbilityState abilityState = this._stateController.AbilityState;
				return abilityState - Scp096AbilityState.Charging <= 1 || base.LockMovement;
			}
		}

		protected override FpcMotor NewMotor
		{
			get
			{
				return new Scp096Motor(base.Hub, base.Role as Scp096Role);
			}
		}

		protected override void UpdateMovement()
		{
			this.UpdateSpeedAndOverrides();
			base.UpdateMovement();
		}

		private void UpdateSpeedAndOverrides()
		{
			Scp096AbilityState abilityState = this._stateController.AbilityState;
			if (abilityState == Scp096AbilityState.Charging)
			{
				this.MovementSpeed = 18.5f;
				(base.Motor as Scp096Motor).SetOverride(base.transform.forward);
				return;
			}
			if (abilityState == Scp096AbilityState.PryingGate)
			{
				this.MovementSpeed = 3.9f;
				this.UpdateGatePrying();
				return;
			}
			switch (this._stateController.RageState)
			{
			case Scp096RageState.Distressed:
			case Scp096RageState.Calming:
				this.MovementSpeed = 2.55f;
				this.JumpSpeed = this._normalJumpSpeed;
				return;
			case Scp096RageState.Enraged:
				this.MovementSpeed = 8f;
				this.JumpSpeed = this._jumpSpeedRage;
				return;
			default:
				this.MovementSpeed = 3.9f;
				this.JumpSpeed = this._normalJumpSpeed;
				return;
			}
		}

		private void UpdateGatePrying()
		{
			float num = (float)this._gatePrySw.Elapsed.TotalSeconds;
			if (num > 2f)
			{
				if (NetworkServer.active)
				{
					this._stateController.SetAbilityState(Scp096AbilityState.None);
				}
				return;
			}
			if (!base.Role.IsLocalPlayer)
			{
				return;
			}
			num /= 2f;
			base.Position = new Vector3(this._gatePryX.Evaluate(num), base.Position.y, this._gatePryZ.Evaluate(num));
			base.MouseLook.CurrentHorizontal = Mathf.MoveTowardsAngle(base.MouseLook.CurrentHorizontal, this._gateLookAngle, Time.deltaTime * 120f);
			base.MouseLook.CurrentVertical = Mathf.MoveTowardsAngle(base.MouseLook.CurrentVertical, 0f, Time.deltaTime * 120f);
		}

		private void SetGatePryCurves(int index, Vector3 pos)
		{
			Keyframe[] keys = this._gatePryX.keys;
			keys[index].value = pos.x;
			this._gatePryX.keys = keys;
			Keyframe[] keys2 = this._gatePryZ.keys;
			keys2[index].value = pos.z;
			this._gatePryZ.keys = keys2;
		}

		private void Awake()
		{
			this._normalJumpSpeed = this.JumpSpeed;
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			this._stateController = (base.Role as Scp096Role).StateController;
		}

		public void SetTargetGate(PryableDoor door)
		{
			if (door.PryPositions.Length == 0)
			{
				return;
			}
			this._gatePrySw.Restart();
			if (base.Role.IsLocalPlayer)
			{
				this.SetGatePryCurves(0, base.Position);
				this._pryablePoints.Clear();
				this._pryablePoints.AddRange(door.PryPositions);
				this._pryablePoints.Sort(delegate(Transform x, Transform y)
				{
					float sqrMagnitude = (base.Position - x.position).sqrMagnitude;
					float sqrMagnitude2 = (base.Position - y.position).sqrMagnitude;
					return sqrMagnitude.CompareTo(sqrMagnitude2);
				});
				Transform transform = this._pryablePoints[0];
				Transform transform2 = this._pryablePoints[this._pryablePoints.Count - 1];
				this.SetGatePryCurves(1, transform.position);
				this.SetGatePryCurves(2, transform2.position);
				Vector3 normalized = (transform2.position - transform.position).normalized;
				this._gateLookAngle = Vector3.Angle(normalized, Vector3.forward) * Mathf.Sign(Vector3.Dot(normalized, Vector3.right));
			}
			if (!NetworkServer.active)
			{
				return;
			}
			this._stateController.SetAbilityState(Scp096AbilityState.PryingGate);
		}

		[SerializeField]
		private float _jumpSpeedRage;

		private const float SlowedSpeed = 2.55f;

		private const float NormalSpeed = 3.9f;

		private const float RageSpeed = 8f;

		private const float ChargeSpeed = 18.5f;

		private const float PryGateDuration = 2f;

		private const float AngleAdjustSpeed = 120f;

		private Scp096StateController _stateController;

		private float _gateLookAngle;

		private float _normalJumpSpeed;

		private readonly Stopwatch _gatePrySw = Stopwatch.StartNew();

		private readonly AnimationCurve _gatePryX = new AnimationCurve(Scp096MovementModule.TemplateKeyframes);

		private readonly AnimationCurve _gatePryZ = new AnimationCurve(Scp096MovementModule.TemplateKeyframes);

		private readonly List<Transform> _pryablePoints = new List<Transform>();

		private static readonly Keyframe[] TemplateKeyframes = new Keyframe[]
		{
			new Keyframe(0f, 1f, 0f, 0f, 0f, 0.3f),
			new Keyframe(0.35f, 0.2f, -0f, -0f, 0.5f, 0.4f),
			new Keyframe(1f, 0f, -0f, -0f, 0.2f, 0f)
		};
	}
}
