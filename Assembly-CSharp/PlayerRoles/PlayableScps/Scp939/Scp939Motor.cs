using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939Motor : FpcMotor
	{
		private bool IsLocalPlayer
		{
			get
			{
				return this.ViewMode == FpcMotor.FpcViewMode.LocalPlayer;
			}
		}

		private bool WantsToLunge
		{
			get
			{
				return this._lunge.IsReady && (this._lunge.LungeRequested || base.WantsToJump);
			}
		}

		private bool IsLunging
		{
			get
			{
				return this._lunge.State == Scp939LungeState.Triggered;
			}
		}

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
				return this.CachedTransform.forward;
			}
		}

		public override Vector3 Velocity
		{
			get
			{
				if (!NetworkServer.active || this.Hub.isLocalPlayer || !this.IsLunging)
				{
					return base.Velocity;
				}
				return this.CachedTransform.forward * this._lunge.LungeForwardSpeed;
			}
		}

		public bool MovingForwards
		{
			get
			{
				return !this.IsLocalPlayer || Vector3.Dot(base.MoveDirection.NormalizeIgnoreY(), this.CachedTransform.forward) > 0.4f;
			}
		}

		private void ProcessHitboxCollision(HitboxIdentity hid)
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(this.Hub, hid.TargetHub))
			{
				return;
			}
			FpcStandardRoleBase fpcStandardRoleBase = hid.TargetHub.roleManager.CurrentRole as FpcStandardRoleBase;
			if (fpcStandardRoleBase == null)
			{
				return;
			}
			if ((this.MainModule.Position - fpcStandardRoleBase.FpcModule.Position).SqrMagnitudeIgnoreY() < 0.4f)
			{
				return;
			}
			Scp939Motor.DetectedTargets.Add(fpcStandardRoleBase);
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
				IDestructible destructible;
				if (Scp939Motor.Detections[i].TryGetComponent<IDestructible>(out destructible))
				{
					HitboxIdentity hitboxIdentity = destructible as HitboxIdentity;
					if (hitboxIdentity != null)
					{
						this.ProcessHitboxCollision(hitboxIdentity);
					}
					else
					{
						BreakableWindow breakableWindow = destructible as BreakableWindow;
						if (breakableWindow != null)
						{
							this.ProcessWindowCollision(breakableWindow);
						}
					}
				}
			}
		}

		protected override void UpdateFloating()
		{
			if (this._focus.State == 1f && this._lunge.State != Scp939LungeState.Triggered)
			{
				Vector3 moveDirection = base.MoveDirection;
				moveDirection.y = Mathf.Min(moveDirection.y, -this._lunge.LungeJumpSpeed);
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
			Vector3 position = this.MainModule.Position;
			Vector3 vector = position + frameMove;
			Vector3 vector2 = Vector3.down * 0.6f;
			this.OverlapCapsule(position, vector);
			this.OverlapCapsule(position + vector2, vector + vector2);
			if (!this.IsLocalPlayer)
			{
				return frameMove;
			}
			FpcStandardRoleBase fpcStandardRoleBase = null;
			float num = float.MaxValue;
			bool flag = false;
			foreach (FpcStandardRoleBase fpcStandardRoleBase2 in Scp939Motor.DetectedTargets)
			{
				float sqrMagnitude = (fpcStandardRoleBase2.FpcModule.Position - vector).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					if (!Physics.Linecast(fpcStandardRoleBase2.FpcModule.Position, vector, PlayerRolesUtils.BlockerMask))
					{
						fpcStandardRoleBase = fpcStandardRoleBase2;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return frameMove;
			}
			this._lunge.ClientSendHit(fpcStandardRoleBase);
			return this._lunge.LungeJumpSpeed * Time.deltaTime * Vector3.down;
		}

		public Scp939Motor(ReferenceHub hub, Scp939Role role)
			: base(hub, role.FpcModule, false)
		{
			this._role = role;
			role.SubroutineModule.TryGetSubroutine<Scp939FocusAbility>(out this._focus);
			role.SubroutineModule.TryGetSubroutine<Scp939LungeAbility>(out this._lunge);
			role.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out this._cloud);
		}

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

		private static readonly CachedLayerMask Mask = new CachedLayerMask(new string[] { "Hitbox", "Glass" });

		private static readonly HashSet<FpcStandardRoleBase> DetectedTargets = new HashSet<FpcStandardRoleBase>();
	}
}
