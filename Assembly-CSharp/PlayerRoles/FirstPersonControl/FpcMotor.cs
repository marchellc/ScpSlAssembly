using System;
using System.Diagnostics;
using CursorManagement;
using Interactables.Interobjects;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcMotor
	{
		public event Action<Vector3> OnBeforeMove;

		public Vector3 MoveDirection { get; protected set; }

		public virtual Vector3 Velocity { get; private set; }

		public bool IsJumping { get; private set; }

		public bool IsInvisible { get; set; }

		public RelativePosition ReceivedPosition { get; set; }

		public bool MovementDetected { get; set; }

		public bool RotationDetected { get; set; }

		public bool WantsToJump
		{
			get
			{
				return this._requestedJump || (this.Hub.isLocalPlayer && Input.GetKeyDown(FpcMotor._keyJump) && !this.InputLocked);
			}
			set
			{
				this._requestedJump = value;
			}
		}

		protected virtual float Speed
		{
			get
			{
				return this.MainModule.MaxMovementSpeed;
			}
		}

		protected virtual Vector3 DesiredMove
		{
			get
			{
				if (this.ViewMode == FpcMotor.FpcViewMode.LocalPlayer)
				{
					Vector3 vector;
					if (this.TryGetOverride(out vector))
					{
						return vector;
					}
					if (this.InputLocked)
					{
						return Vector3.zero;
					}
					float num = 0f;
					float num2 = 0f;
					if (Input.GetKey(FpcMotor._keyFwd))
					{
						num2 += 1f;
					}
					if (Input.GetKey(FpcMotor._keyBwd))
					{
						num2 -= 1f;
					}
					if (Input.GetKey(FpcMotor._keyRgt))
					{
						num += 1f;
					}
					if (Input.GetKey(FpcMotor._keyLft))
					{
						num -= 1f;
					}
					return this.CachedTransform.forward * num2 + this.CachedTransform.right * num;
				}
				else
				{
					Vector3 position = this.ReceivedPosition.Position;
					Vector3 vector2 = position - this.Position;
					if (NetworkServer.active)
					{
						float num3 = Mathf.Clamp(vector2.y * 1.6f, 0f, 0.35f * this.MainModule.JumpSpeed);
						this.MainModule.CharController.stepOffset = Mathf.Min(this._defaultStepOffset + num3, this._defaultHeight);
						if (this.MainModule.Noclip.RecentlyActive)
						{
							this.Position = position;
							return Vector3.zero;
						}
					}
					float num4 = vector2.MagnitudeIgnoreY();
					if (num4 < 0.03f)
					{
						return Vector3.zero;
					}
					num4 -= 0.5f;
					if (num4 >= this._lastMaxSpeed || Mathf.Abs(vector2.y) >= Mathf.Max(this.MainModule.JumpSpeed, Mathf.Abs(this.MoveDirection.y)))
					{
						if (NetworkServer.active)
						{
							if (this._lastOverrideTime.Elapsed.TotalSeconds > 0.4000000059604645)
							{
								this.MainModule.ServerOverridePosition(this.Position);
							}
						}
						else
						{
							this.Position = position;
						}
						return Vector3.zero;
					}
					if (!NetworkServer.active)
					{
						return vector2;
					}
					return new Vector3(vector2.x, 0f, vector2.z);
				}
			}
		}

		private Vector3 Position
		{
			get
			{
				return this.MainModule.Position;
			}
			set
			{
				this.MainModule.Position = value;
			}
		}

		private bool InputLocked
		{
			get
			{
				return CursorManager.MovementLocked;
			}
		}

		public FpcMotor(ReferenceHub hub, FirstPersonMovementModule module, bool enableFallDamage)
		{
			this.Hub = hub;
			this.MainModule = module;
			this.GravityController = new FpcGravityController(this);
			this._enableFallDamage = enableFallDamage;
			this.CachedTransform = hub.transform;
			if (NetworkServer.active)
			{
				this._lastOverrideTime = Stopwatch.StartNew();
				this._fallDamageImmunity = Stopwatch.StartNew();
				module.OnServerPositionOverwritten = (Action)Delegate.Combine(module.OnServerPositionOverwritten, new Action(delegate
				{
					this._lastOverrideTime.Restart();
				}));
				this._defaultStepOffset = module.CharController.stepOffset;
				this._defaultHeight = module.CharController.height;
			}
			if (this.Hub.isLocalPlayer)
			{
				this.ViewMode = FpcMotor.FpcViewMode.LocalPlayer;
				FpcMotor.ReloadInputConfigs();
				if (!FpcMotor._reloadKeysEventSet)
				{
					NewInput.OnAnyModified += FpcMotor.ReloadInputConfigs;
					FpcMotor._reloadKeysEventSet = true;
					return;
				}
			}
			else
			{
				this.ViewMode = (NetworkServer.active ? FpcMotor.FpcViewMode.Server : FpcMotor.FpcViewMode.Thirdperson);
			}
		}

		public void UpdatePosition(out bool sendJump)
		{
			sendJump = false;
			bool flag = this.Speed < 0f;
			this._lastMaxSpeed = Mathf.Abs(this.Speed);
			if (this.MainModule.Noclip.IsActive)
			{
				this.MoveDirection = Vector3.zero;
				return;
			}
			if (this.ViewMode == FpcMotor.FpcViewMode.Thirdperson)
			{
				this.UpdateThirdperson();
				return;
			}
			CharacterController charController = this.MainModule.CharController;
			RaycastHit raycastHit;
			Physics.SphereCast(this.Position, charController.radius, Vector3.down, out raycastHit, charController.height / 2f, FpcStateProcessor.Mask, QueryTriggerInteraction.Ignore);
			Vector3 vector = Vector3.ProjectOnPlane(this.DesiredMove, raycastHit.normal).normalized;
			if (this.ViewMode == FpcMotor.FpcViewMode.LocalPlayer && flag)
			{
				vector = -vector;
			}
			this.MoveDirection = new Vector3(vector.x * this._lastMaxSpeed, this.MoveDirection.y, vector.z * this._lastMaxSpeed);
			if (charController.isGrounded)
			{
				this.UpdateGrounded(ref sendJump, this.MainModule.JumpSpeed);
				return;
			}
			this.UpdateFloating();
		}

		public void ResetFallDamageCooldown()
		{
			this._fallDamageImmunity.Restart();
		}

		protected virtual Vector3 GetFrameMove()
		{
			if (this.MainModule.Noclip.IsActive)
			{
				return Vector3.zero;
			}
			Vector3 vector = this.MoveDirection * Time.deltaTime;
			if (this.ViewMode != FpcMotor.FpcViewMode.LocalPlayer)
			{
				Vector3 position = this.ReceivedPosition.Position;
				Vector3 position2 = this.Position;
				vector.x = FpcMotor.ClampMoveDirection(position2.x, position.x, vector.x);
				vector.z = FpcMotor.ClampMoveDirection(position2.z, position.z, vector.z);
			}
			return vector;
		}

		protected virtual void Move()
		{
			CharacterController charController = this.MainModule.CharController;
			Vector3 position = this.Position;
			Vector3 frameMove = this.GetFrameMove();
			Action<Vector3> onBeforeMove = this.OnBeforeMove;
			if (onBeforeMove != null)
			{
				onBeforeMove(frameMove);
			}
			charController.Move(frameMove);
			this.Position = this.CachedTransform.position;
			this.MovementDetected = this.Position != position;
			this.Velocity = (this.Position - position) / Time.deltaTime;
			this.MainModule.IsGrounded = charController.isGrounded;
		}

		protected virtual void UpdateGrounded(ref bool sendJump, float jumpSpeed)
		{
			Vector3 moveDirection = this.MoveDirection;
			bool flag = false;
			if (this.WantsToJump)
			{
				if (jumpSpeed > 0f)
				{
					moveDirection.y = jumpSpeed;
					flag = true;
				}
				this._requestedJump = false;
				this.IsJumping = true;
				sendJump = true;
			}
			else
			{
				moveDirection.y = -10f;
				this.IsJumping = false;
			}
			this.MoveDirection = moveDirection;
			if (this._maxFallSpeed > 14.5f && this._enableFallDamage)
			{
				this.ServerProcessFall(this._maxFallSpeed - 14.5f);
			}
			this._maxFallSpeed = 14.5f;
			if (flag)
			{
				this.UpdateFloating();
				return;
			}
			this.Move();
			if (this.MainModule.CharController.isGrounded)
			{
				return;
			}
			this.MoveDirection = Vector3.Scale(this.MoveDirection, new Vector3(1f, 0f, 1f));
		}

		protected virtual void UpdateFloating()
		{
			Vector3 vector = 0.5f * Time.deltaTime * this.GravityController.Gravity;
			this.MoveDirection += vector;
			this.Move();
			this.MoveDirection += vector;
			this._maxFallSpeed = Mathf.Max(this._maxFallSpeed, -this.MoveDirection.y);
		}

		private static float ClampMoveDirection(float curPos, float targetPos, float moveDir)
		{
			float num = Mathf.Abs(curPos - targetPos);
			return Mathf.Clamp(moveDir, -num, num);
		}

		private void UpdateThirdperson()
		{
			if (this.IsInvisible)
			{
				this.Position = FpcMotor.InvisiblePosition;
				return;
			}
			Vector3 desiredMove = this.DesiredMove;
			Vector3 position = this.ReceivedPosition.Position;
			Vector3 position2 = this.Position;
			Vector3 vector = new Vector3(position.x, position2.y, position.z);
			float num = Time.deltaTime * Mathf.Max(3f, this._lastMaxSpeed);
			Vector3 vector2 = Vector3.Lerp(position2, vector, num * 2f);
			vector2.y = Mathf.Lerp(vector2.y, position.y, 9f * Time.deltaTime);
			this.Position = vector2;
			this.Velocity = (vector2 - position2) / Time.deltaTime;
			this.MoveDirection = new Vector3(desiredMove.x, 0f, desiredMove.z).normalized * this._lastMaxSpeed + Vector3.up * desiredMove.y;
		}

		private void ServerProcessFall(float speed)
		{
			if (!NetworkServer.active || this._fallDamageImmunity.Elapsed.TotalSeconds < 2.5)
			{
				return;
			}
			RoleTypeId roleId = this.Hub.GetRoleId();
			Vector3 position = this.Position;
			float num = Mathf.Pow(speed, 0.8f) * 31.4f + 10f;
			this.Hub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Falldown, null));
			new FpcFallDamageMessage(this.Hub, position, roleId).SendToAuthenticated(0);
		}

		public void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamb, Vector3 deltaPos, Quaternion deltaRot)
		{
			if (!elevatorBounds.Contains(this.Position))
			{
				return;
			}
			if (NetworkServer.active)
			{
				Vector3 position = this.ReceivedPosition.Position;
				Vector3 vector = new Vector3(position.x, this.Position.y, position.z);
				if (elevatorBounds.Contains(vector))
				{
					this._lastOverrideTime.Restart();
				}
			}
			Transform transform = chamb.transform;
			Vector3 vector2 = this.Position + deltaPos;
			vector2 = deltaRot * (vector2 - transform.position) + transform.position;
			this.Position = vector2;
			if (this.Hub.isLocalPlayer)
			{
				this.MainModule.MouseLook.CurrentHorizontal += deltaRot.eulerAngles.y;
			}
		}

		private bool TryGetOverride(out Vector3 overrideDir)
		{
			bool flag = false;
			overrideDir = Vector3.zero;
			IMovementInputOverride movementInputOverride = this.Hub.inventory.CurInstance.GetMobilityController() as IMovementInputOverride;
			if (movementInputOverride != null && movementInputOverride.MovementOverrideActive)
			{
				flag = true;
				overrideDir = movementInputOverride.MovementOverrideDirection;
			}
			for (int i = 0; i < this.Hub.playerEffectsController.EffectsLength; i++)
			{
				IMovementInputOverride movementInputOverride2 = this.Hub.playerEffectsController.AllEffects[i] as IMovementInputOverride;
				if (movementInputOverride2 != null && movementInputOverride2.MovementOverrideActive)
				{
					flag = true;
					overrideDir += movementInputOverride2.MovementOverrideDirection;
				}
			}
			return flag;
		}

		private static void ReloadInputConfigs()
		{
		}

		public readonly ReferenceHub Hub;

		protected readonly Transform CachedTransform;

		protected readonly FpcMotor.FpcViewMode ViewMode;

		protected readonly FirstPersonMovementModule MainModule;

		public readonly FpcGravityController GravityController;

		private static readonly Vector3 InvisiblePosition = Vector3.up * 6000f;

		private const float JumpToStepOffsetRatio = 0.35f;

		private const float StepDiffMultiplier = 1.6f;

		private const float StickToGroundForce = 10f;

		private const float MinMoveDiff = 0.03f;

		private const float PositionOverrideAbsTolerance = 0.5f;

		private const float PositionOverrideCooldown = 0.4f;

		private const float ThirdpersonLerpMultiplier = 2f;

		private const float ThirdpersonHeightLerp = 9f;

		private const float ThirdpersonMinSpeed = 3f;

		private const float FallDamageMinVelocity = 14.5f;

		private const float FallDamagePower = 0.8f;

		private const float FallDamageMultiplier = 31.4f;

		private const float FallDamageAbsolute = 10f;

		private const float FallDamageImmunityTime = 2.5f;

		private bool _requestedJump;

		private float _lastMaxSpeed;

		private float _maxFallSpeed;

		private static KeyCode _keyFwd;

		private static KeyCode _keyBwd;

		private static KeyCode _keyLft;

		private static KeyCode _keyRgt;

		private static KeyCode _keyJump;

		private static bool _reloadKeysEventSet;

		private readonly Stopwatch _lastOverrideTime;

		private readonly Stopwatch _fallDamageImmunity;

		private readonly bool _enableFallDamage;

		private readonly float _defaultStepOffset;

		private readonly float _defaultHeight;

		protected enum FpcViewMode
		{
			LocalPlayer,
			Server,
			Thirdperson
		}
	}
}
