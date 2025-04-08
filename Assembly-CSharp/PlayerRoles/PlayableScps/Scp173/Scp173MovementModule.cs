using System;
using System.Diagnostics;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173
{
	public class Scp173MovementModule : FirstPersonMovementModule
	{
		private float MovementSpeed
		{
			set
			{
				this.SneakSpeed = value;
				this.WalkSpeed = value;
				this.SprintSpeed = value;
				this.JumpSpeed = ((value < this._normalSpeed) ? 0f : this._jumpSpeed);
			}
		}

		private float TargetSpeed
		{
			get
			{
				if (this._observersTracker.IsObserved)
				{
					return 0f;
				}
				if (!this._breakneckSpeeds.IsActive)
				{
					return this._normalSpeed;
				}
				return this._fastSpeed;
			}
		}

		private float ServerSpeed
		{
			get
			{
				float targetSpeed = this.TargetSpeed;
				if (targetSpeed > 0f)
				{
					this._lookStopwatch.Restart();
					return targetSpeed;
				}
				if (this._lookStopwatch.Elapsed.TotalSeconds >= 0.4000000059604645)
				{
					return 0f;
				}
				return this._normalSpeed;
			}
		}

		private static int TpMask
		{
			get
			{
				if (Scp173MovementModule._snapMask != 0)
				{
					return Scp173MovementModule._snapMask;
				}
				int num = LayerMask.NameToLayer("Player");
				for (int i = 0; i < 32; i++)
				{
					if (!Physics.GetIgnoreLayerCollision(num, i))
					{
						Scp173MovementModule._snapMask |= 1 << i;
					}
				}
				return Scp173MovementModule._snapMask;
			}
		}

		private void Awake()
		{
			this._normalSpeed = this.WalkSpeed;
			this._fastSpeed = this.SprintSpeed;
			this._jumpSpeed = this.JumpSpeed;
			this._observerSpeed = this.SprintSpeed * 2f;
			this._role = base.GetComponent<Scp173Role>();
			this._role.SubroutineModule.TryGetSubroutine<Scp173BreakneckSpeedsAbility>(out this._breakneckSpeeds);
			this._role.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out this._observersTracker);
		}

		protected override void UpdateMovement()
		{
			this.MovementSpeed = (this._role.IsLocalPlayer ? this.TargetSpeed : (NetworkServer.active ? this.ServerSpeed : this._observerSpeed));
			base.UpdateMovement();
			this.UpdateGlassBreaking();
		}

		private void UpdateGlassBreaking()
		{
			if (!NetworkServer.active || !this._breakneckSpeeds.IsActive)
			{
				return;
			}
			Vector3 moveDirection = base.Motor.MoveDirection;
			float num = base.CharController.radius + 0.3f;
			RaycastHit raycastHit;
			if (!Physics.Raycast(base.Position, moveDirection, out raycastHit, num, 16384))
			{
				return;
			}
			BreakableWindow breakableWindow;
			if (!raycastHit.collider.TryGetComponent<BreakableWindow>(out breakableWindow))
			{
				return;
			}
			breakableWindow.Damage(breakableWindow.health, this._role.DamageHandler, Vector3.zero);
		}

		public bool TryGetTeleportPos(float maxDis, out Vector3 pos, out float usedDistance)
		{
			Vector3 position = base.Hub.PlayerCameraReference.position;
			Vector3 forward = base.Hub.PlayerCameraReference.forward;
			RaycastHit raycastHit;
			if (!Physics.SphereCast(position, 0.025f, forward, out raycastHit, maxDis, Scp173MovementModule.TpMask))
			{
				raycastHit.point = position + forward * maxDis;
				raycastHit.normal = Vector3.up;
				raycastHit.distance = maxDis;
			}
			usedDistance = raycastHit.distance;
			return this.CheckTeleportPosition(raycastHit, out pos);
		}

		private bool CheckTeleportPosition(RaycastHit hit, out Vector3 groundPoint)
		{
			groundPoint = Vector3.zero;
			float radius = this.CharacterControllerSettings.Radius;
			float num = radius * 1.2f;
			Vector3 vector = hit.point + hit.normal * num;
			if (Physics.CheckSphere(vector, radius, Scp173MovementModule.TpMask))
			{
				return false;
			}
			if (Physics.Raycast(hit.point, hit.normal, num, Scp173MovementModule.TpMask))
			{
				return false;
			}
			RaycastHit raycastHit;
			if (!Physics.SphereCast(vector, radius, Vector3.down, out raycastHit, 3.6f, Scp173MovementModule.TpMask))
			{
				return false;
			}
			if (!Physics.SphereCast(new Ray(vector, Vector3.down), radius * 0.5f, raycastHit.distance + 0.6f, Scp173MovementModule.TpMask))
			{
				return false;
			}
			if (Vector3.Dot(Vector3.up, raycastHit.normal) < 0.15f)
			{
				return false;
			}
			RaycastHit raycastHit2;
			if (!Physics.SphereCast(vector, radius, Vector3.up, out raycastHit2, 7.2f, Scp173MovementModule.TpMask))
			{
				raycastHit2.point = vector + Vector3.up * 7.2f;
			}
			if (Mathf.Abs(raycastHit.point.y - raycastHit2.point.y) < this.CharacterControllerSettings.Height)
			{
				return false;
			}
			PitKiller pitKiller;
			if (raycastHit.collider.TryGetComponent<PitKiller>(out pitKiller))
			{
				return false;
			}
			groundPoint = raycastHit.point + (raycastHit.normal + Vector3.down) * radius;
			return true;
		}

		public void ServerTeleportTo(Vector3 pos)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.ServerOverridePosition(pos);
		}

		private float _normalSpeed;

		private float _fastSpeed;

		private float _observerSpeed;

		private float _jumpSpeed;

		private Scp173Role _role;

		private Scp173BreakneckSpeedsAbility _breakneckSpeeds;

		private Scp173ObserversTracker _observersTracker;

		private static int _snapMask;

		private readonly Stopwatch _lookStopwatch = Stopwatch.StartNew();

		private const float ObserverSpeedMultiplier = 2f;

		private const float ServerStopTime = 0.4f;

		private const int GlassLayerMask = 16384;

		private const float GlassRaycastDis = 0.3f;

		private const float RaycastFloorHeight = 3.6f;

		private const float RaycastCeilHeight = 7.2f;

		private const float RaycastPilotRadius = 0.025f;

		private const float RaycastFloorDot = 0.15f;

		private const float RaycastCcRadiusMultiplier = 1.2f;

		private const float RaycastStabilityRadiusRatio = 0.5f;

		private const float RaycastStabilityDistance = 0.6f;
	}
}
