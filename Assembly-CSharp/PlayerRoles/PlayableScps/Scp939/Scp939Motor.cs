using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939Motor : FpcMotor
{
	private const float MinDot = 0.4f;

	private const float LungeRadius = 0.6f;

	private const float FocusToleranceOffset = 0.13f;

	private const float FocusTimeTolerance = 3.5f;

	private const int MaxDetections = 32;

	private const float MinDistanceSqr = 0.4f;

	private readonly Scp939Role _role;

	private readonly Scp939FocusAbility _focus;

	private readonly Scp939LungeAbility _lunge;

	private readonly Scp939AmnesticCloudAbility _cloud;

	private static readonly Collider[] Detections = new Collider[32];

	private static readonly CachedLayerMask Mask = new CachedLayerMask("Hitbox", "Glass");

	private static readonly HashSet<FpcStandardRoleBase> DetectedTargets = new HashSet<FpcStandardRoleBase>();

	private bool IsLocalPlayer => base.ViewMode == FpcViewMode.LocalPlayer;

	private bool IsControllable
	{
		get
		{
			if (!this.IsLocalPlayer)
			{
				if (NetworkServer.active)
				{
					return base.Hub.IsDummy;
				}
				return false;
			}
			return true;
		}
	}

	private bool WantsToLunge
	{
		get
		{
			if (this._lunge.IsReady)
			{
				if (!this._lunge.LungeRequested)
				{
					return base.WantsToJump;
				}
				return true;
			}
			return false;
		}
	}

	private bool IsLunging => this._lunge.State == Scp939LungeState.Triggered;

	protected override float Speed
	{
		get
		{
			if (this.IsLunging)
			{
				return this._lunge.LungeForwardSpeed;
			}
			if (this.IsLocalPlayer && this._cloud.TargetState)
			{
				return base.Speed * Mathf.Clamp01(1f - this._cloud.HoldDuration);
			}
			float num = this._focus.State;
			if (NetworkServer.active && !this.IsLocalPlayer && this._focus.TargetState)
			{
				if (this._focus.FrozenTime > 3.5f)
				{
					return 0f;
				}
				num -= 0.13f;
			}
			return base.Speed * Mathf.Clamp01(1f - num);
		}
	}

	protected override Vector3 DesiredMove
	{
		get
		{
			if (!this.IsLocalPlayer || !this.IsLunging)
			{
				return base.DesiredMove;
			}
			return base.CachedTransform.forward;
		}
	}

	public override Vector3 Velocity
	{
		get
		{
			if (!NetworkServer.active || base.Hub.isLocalPlayer || !this.IsLunging)
			{
				return base.Velocity;
			}
			return base.CachedTransform.forward * this._lunge.LungeForwardSpeed;
		}
	}

	public bool MovingForwards
	{
		get
		{
			if (this.IsLocalPlayer)
			{
				return Vector3.Dot(base.MoveDirection.NormalizeIgnoreY(), base.CachedTransform.forward) > 0.4f;
			}
			return true;
		}
	}

	private void ProcessHitboxCollision(HitboxIdentity hid)
	{
		if (this.IsControllable && HitboxIdentity.IsEnemy(base.Hub, hid.TargetHub) && hid.TargetHub.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase && !((base.MainModule.Position - fpcStandardRoleBase.FpcModule.Position).SqrMagnitudeIgnoreY() < 0.4f))
		{
			Scp939Motor.DetectedTargets.Add(fpcStandardRoleBase);
		}
	}

	private void ProcessWindowCollision(BreakableWindow window)
	{
		if (NetworkServer.active)
		{
			window.Damage(window.health, new Scp939DamageHandler(this._role, 120f, Scp939DamageType.LungeTarget), default(Vector3));
			return;
		}
		Collider[] componentsInChildren = window.GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
	}

	private void OverlapCapsule(Vector3 point1, Vector3 point2)
	{
		int num = Physics.OverlapCapsuleNonAlloc(point1, point2, 0.6f, Scp939Motor.Detections, Scp939Motor.Mask);
		for (int i = 0; i < num; i++)
		{
			if (Scp939Motor.Detections[i].TryGetComponent<IDestructible>(out var component))
			{
				if (component is HitboxIdentity hid)
				{
					this.ProcessHitboxCollision(hid);
				}
				else if (component is BreakableWindow window)
				{
					this.ProcessWindowCollision(window);
				}
			}
		}
	}

	protected override void UpdateFloating()
	{
		if (this._focus.State == 1f && this._lunge.State != Scp939LungeState.Triggered)
		{
			Vector3 moveDirection = base.MoveDirection;
			moveDirection.y = Mathf.Min(moveDirection.y, 0f - this._lunge.LungeJumpSpeed);
			base.MoveDirection = moveDirection;
		}
		base.UpdateFloating();
	}

	protected override void UpdateGrounded(ref bool sendJump, float jumpSpeed)
	{
		if (this.WantsToLunge)
		{
			this._lunge.TriggerLunge();
			jumpSpeed = this._lunge.LungeJumpSpeed;
			base.WantsToJump = true;
		}
		else if (this._focus.State > 0f || this._cloud.TargetState)
		{
			jumpSpeed = 0f;
		}
		base.UpdateGrounded(ref sendJump, jumpSpeed);
	}

	protected override Vector3 GetFrameMove()
	{
		Vector3 frameMove = base.GetFrameMove();
		if (!this.IsLunging || (!NetworkServer.active && !this.IsLocalPlayer))
		{
			return frameMove;
		}
		Scp939Motor.DetectedTargets.Clear();
		Vector3 position = base.MainModule.Position;
		Vector3 vector = position + frameMove;
		Vector3 vector2 = Vector3.down * 0.6f;
		this.OverlapCapsule(position, vector);
		this.OverlapCapsule(position + vector2, vector + vector2);
		if (!this.IsControllable)
		{
			return frameMove;
		}
		FpcStandardRoleBase targetRole = null;
		float num = float.MaxValue;
		bool flag = false;
		foreach (FpcStandardRoleBase detectedTarget in Scp939Motor.DetectedTargets)
		{
			float sqrMagnitude = (detectedTarget.FpcModule.Position - vector).sqrMagnitude;
			if (!(sqrMagnitude >= num))
			{
				num = sqrMagnitude;
				if (!Physics.Linecast(detectedTarget.FpcModule.Position, vector, PlayerRolesUtils.AttackMask))
				{
					targetRole = detectedTarget;
					flag = true;
				}
			}
		}
		if (!flag)
		{
			return frameMove;
		}
		this._lunge.ClientSendHit(targetRole);
		return this._lunge.LungeJumpSpeed * Time.deltaTime * Vector3.down;
	}

	public Scp939Motor(ReferenceHub hub, Scp939Role role, FallDamageSettings fallDamageSettings)
		: base(hub, role.FpcModule, fallDamageSettings)
	{
		this._role = role;
		role.SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out this._focus);
		role.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out this._lunge);
		role.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out this._cloud);
	}
}
