using System.Diagnostics;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp173;

public class Scp173MovementModule : FirstPersonMovementModule
{
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

	private float MovementSpeed
	{
		set
		{
			base.SneakSpeed = value;
			base.WalkSpeed = value;
			base.SprintSpeed = value;
			base.JumpSpeed = ((value < this._normalSpeed) ? 0f : this._jumpSpeed);
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
			if (!(this._lookStopwatch.Elapsed.TotalSeconds < 0.4000000059604645))
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
			int layer = LayerMask.NameToLayer("Player");
			for (int i = 0; i < 32; i++)
			{
				if (!Physics.GetIgnoreLayerCollision(layer, i))
				{
					Scp173MovementModule._snapMask |= 1 << i;
				}
			}
			return Scp173MovementModule._snapMask;
		}
	}

	private void Awake()
	{
		this._normalSpeed = base.WalkSpeed;
		this._fastSpeed = base.SprintSpeed;
		this._jumpSpeed = base.JumpSpeed;
		this._observerSpeed = base.SprintSpeed * 2f;
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
		if (NetworkServer.active && this._breakneckSpeeds.IsActive)
		{
			Vector3 moveDirection = base.Motor.MoveDirection;
			float maxDistance = base.CharController.radius + 0.3f;
			if (Physics.Raycast(base.Position, moveDirection, out var hitInfo, maxDistance, 16384) && hitInfo.collider.TryGetComponent<BreakableWindow>(out var component))
			{
				component.Damage(component.health, this._role.DamageHandler, Vector3.zero);
			}
		}
	}

	public bool TryGetTeleportPos(float maxDis, out Vector3 pos, out float usedDistance)
	{
		Vector3 position = base.Hub.PlayerCameraReference.position;
		Vector3 forward = base.Hub.PlayerCameraReference.forward;
		if (!Physics.SphereCast(position, 0.025f, forward, out var hitInfo, maxDis, Scp173MovementModule.TpMask))
		{
			hitInfo.point = position + forward * maxDis;
			hitInfo.normal = Vector3.up;
			hitInfo.distance = maxDis;
		}
		usedDistance = hitInfo.distance;
		return this.CheckTeleportPosition(hitInfo, out pos);
	}

	private bool CheckTeleportPosition(RaycastHit hit, out Vector3 groundPoint)
	{
		groundPoint = Vector3.zero;
		float radius = base.CharacterControllerSettings.Radius;
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
		if (!Physics.SphereCast(vector, radius, Vector3.down, out var hitInfo, 3.6f, Scp173MovementModule.TpMask))
		{
			return false;
		}
		if (!Physics.SphereCast(new Ray(vector, Vector3.down), radius * 0.5f, hitInfo.distance + 0.6f, Scp173MovementModule.TpMask))
		{
			return false;
		}
		if (Vector3.Dot(Vector3.up, hitInfo.normal) < 0.15f)
		{
			return false;
		}
		if (!Physics.SphereCast(vector, radius, Vector3.up, out var hitInfo2, 7.2f, Scp173MovementModule.TpMask))
		{
			hitInfo2.point = vector + Vector3.up * 7.2f;
		}
		if (Mathf.Abs(hitInfo.point.y - hitInfo2.point.y) < base.CharacterControllerSettings.Height)
		{
			return false;
		}
		if (hitInfo.collider.TryGetComponent<PitKiller>(out var _))
		{
			return false;
		}
		groundPoint = hitInfo.point + (hitInfo.normal + Vector3.down) * radius;
		return true;
	}

	public void ServerTeleportTo(Vector3 pos)
	{
		if (NetworkServer.active)
		{
			base.ServerOverridePosition(pos);
		}
	}
}
