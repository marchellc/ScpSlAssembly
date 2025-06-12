using System;
using System.Diagnostics;
using CursorManagement;
using Interactables.Interobjects;
using InventorySystem.Items;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl;

public class FpcMotor : IDummyActionProvider
{
	protected enum FpcViewMode
	{
		LocalPlayer,
		Server,
		Thirdperson
	}

	public static readonly Vector3 InvisiblePosition = Vector3.up * 6000f;

	public readonly ReferenceHub Hub;

	protected readonly Transform CachedTransform;

	protected readonly FpcViewMode ViewMode;

	protected readonly FirstPersonMovementModule MainModule;

	public readonly FpcGravityController GravityController;

	public readonly FpcScaleController ScaleController;

	private const float JumpToStepOffsetRatio = 0.35f;

	private const float StepDiffMultiplier = 1.6f;

	private const float StickToGroundForce = 10f;

	private const float MinMoveDiff = 0.03f;

	private const float PositionOverrideAbsTolerance = 0.5f;

	private const float PositionOverrideCooldown = 0.4f;

	private const float ThirdpersonLerpMultiplier = 2f;

	private const float ThirdpersonHeightLerp = 9f;

	private const float ThirdpersonMinSpeed = 3f;

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

	private readonly FallDamageSettings _fallDamageSettings;

	private readonly float _defaultStepOffset;

	private readonly float _defaultHeight;

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
			if (!this._requestedJump)
			{
				if (this.Hub.isLocalPlayer && Input.GetKeyDown(FpcMotor._keyJump))
				{
					return !this.InputLocked;
				}
				return false;
			}
			return true;
		}
		set
		{
			this._requestedJump = value;
		}
	}

	protected virtual float Speed => this.MainModule.MaxMovementSpeed;

	protected virtual Vector3 DesiredMove
	{
		get
		{
			if (this.ViewMode == FpcViewMode.LocalPlayer)
			{
				if (this.TryGetOverride(out var overrideDir))
				{
					return overrideDir;
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
			Vector3 position = this.ReceivedPosition.Position;
			Vector3 vector = position - this.Position;
			if (NetworkServer.active)
			{
				float num3 = Mathf.Clamp(vector.y * 1.6f, 0f, 0.35f * this.MainModule.JumpSpeed);
				this.MainModule.CharController.stepOffset = Mathf.Min(this._defaultStepOffset + num3, this._defaultHeight);
				if (this.MainModule.Noclip.RecentlyActive)
				{
					this.Position = position;
					return Vector3.zero;
				}
			}
			float num4 = vector.MagnitudeIgnoreY();
			if (num4 < 0.03f)
			{
				return Vector3.zero;
			}
			num4 -= 0.5f;
			if (num4 < this._lastMaxSpeed && Mathf.Abs(vector.y) < Mathf.Max(this.MainModule.JumpSpeed, Mathf.Abs(this.MoveDirection.y)))
			{
				if (!NetworkServer.active)
				{
					return vector;
				}
				return new Vector3(vector.x, 0f, vector.z);
			}
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

	private bool InputLocked => CursorManager.MovementLocked;

	public event Action<Vector3> OnBeforeMove;

	public FpcMotor(ReferenceHub hub, FirstPersonMovementModule module, FallDamageSettings fallDamageSettings)
	{
		this.Hub = hub;
		this.MainModule = module;
		this.GravityController = new FpcGravityController(this);
		this.ScaleController = new FpcScaleController(this);
		this._fallDamageSettings = fallDamageSettings;
		this.CachedTransform = hub.transform;
		if (NetworkServer.active)
		{
			this._lastOverrideTime = Stopwatch.StartNew();
			this._fallDamageImmunity = Stopwatch.StartNew();
			module.OnServerPositionOverwritten = (Action)Delegate.Combine(module.OnServerPositionOverwritten, (Action)delegate
			{
				this._lastOverrideTime.Restart();
			});
			this._defaultStepOffset = module.CharController.stepOffset;
			this._defaultHeight = module.CharController.height;
		}
		if (this.Hub.isLocalPlayer)
		{
			this.ViewMode = FpcViewMode.LocalPlayer;
			FpcMotor.ReloadInputConfigs();
			if (!FpcMotor._reloadKeysEventSet)
			{
				NewInput.OnAnyModified += ReloadInputConfigs;
				FpcMotor._reloadKeysEventSet = true;
			}
		}
		else
		{
			this.ViewMode = (NetworkServer.active ? FpcViewMode.Server : FpcViewMode.Thirdperson);
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
		if (this.ViewMode == FpcViewMode.Thirdperson)
		{
			this.UpdateThirdperson();
			return;
		}
		CharacterController charController = this.MainModule.CharController;
		Physics.SphereCast(this.Position, charController.radius, Vector3.down, out var hitInfo, charController.height / 2f, FpcStateProcessor.Mask, QueryTriggerInteraction.Ignore);
		Vector3 vector = Vector3.ProjectOnPlane(this.DesiredMove, hitInfo.normal).normalized;
		if (this.ViewMode == FpcViewMode.LocalPlayer && flag)
		{
			vector = -vector;
		}
		this.MoveDirection = new Vector3(vector.x * this._lastMaxSpeed, this.MoveDirection.y, vector.z * this._lastMaxSpeed);
		if (charController.isGrounded)
		{
			this.UpdateGrounded(ref sendJump, this.MainModule.JumpSpeed);
		}
		else
		{
			this.UpdateFloating();
		}
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
		Vector3 result = this.MoveDirection * Time.deltaTime;
		if (this.ViewMode != FpcViewMode.LocalPlayer)
		{
			Vector3 position = this.ReceivedPosition.Position;
			Vector3 position2 = this.Position;
			result.x = FpcMotor.ClampMoveDirection(position2.x, position.x, result.x);
			result.z = FpcMotor.ClampMoveDirection(position2.z, position.z, result.z);
		}
		return result;
	}

	protected virtual void Move()
	{
		CharacterController charController = this.MainModule.CharController;
		Vector3 position = this.Position;
		Vector3 frameMove = this.GetFrameMove();
		this.OnBeforeMove?.Invoke(frameMove);
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
		if (this._fallDamageSettings.Enabled && this._maxFallSpeed > this._fallDamageSettings.MinVelocity)
		{
			this.ServerProcessFall(this._maxFallSpeed - this._fallDamageSettings.MinVelocity);
		}
		this._maxFallSpeed = this._fallDamageSettings.MinVelocity;
		if (flag)
		{
			this.UpdateFloating();
			return;
		}
		this.Move();
		if (!this.MainModule.CharController.isGrounded)
		{
			this.MoveDirection = Vector3.Scale(this.MoveDirection, new Vector3(1f, 0f, 1f));
		}
	}

	protected virtual void UpdateFloating()
	{
		Vector3 vector = 0.5f * Time.deltaTime * this.GravityController.Gravity;
		this.MoveDirection += vector;
		this.Move();
		this.MoveDirection += vector;
		this._maxFallSpeed = Mathf.Max(this._maxFallSpeed, 0f - this.MoveDirection.y);
	}

	private static float ClampMoveDirection(float curPos, float targetPos, float moveDir)
	{
		float num = Mathf.Abs(curPos - targetPos);
		return Mathf.Clamp(moveDir, 0f - num, num);
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
		Vector3 b = new Vector3(position.x, position2.y, position.z);
		float num = Time.deltaTime * Mathf.Max(3f, this._lastMaxSpeed);
		Vector3 vector = Vector3.Lerp(position2, b, num * 2f);
		vector.y = Mathf.Lerp(vector.y, position.y, 9f * Time.deltaTime);
		this.Position = vector;
		this.Velocity = (vector - position2) / Time.deltaTime;
		this.MoveDirection = new Vector3(desiredMove.x, 0f, desiredMove.z).normalized * this._lastMaxSpeed + Vector3.up * desiredMove.y;
	}

	private void ServerProcessFall(float speed)
	{
		if (NetworkServer.active && !(this._fallDamageImmunity.Elapsed.TotalSeconds < (double)this._fallDamageSettings.ImmunityTime))
		{
			PlayerRoleBase currentRole = this.Hub.roleManager.CurrentRole;
			if (!(currentRole is Scp106Role { IsStalking: not false }))
			{
				RoleTypeId roleTypeId = currentRole.RoleTypeId;
				Vector3 position = this.Position;
				float damage = this._fallDamageSettings.CalculateDamage(speed);
				this.Hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Falldown));
				new FpcFallDamageMessage(this.Hub, position, roleTypeId).SendToAuthenticated();
			}
		}
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
			Vector3 point = new Vector3(position.x, this.Position.y, position.z);
			if (elevatorBounds.Contains(point))
			{
				this._lastOverrideTime.Restart();
			}
		}
		Transform transform = chamb.transform;
		Vector3 vector = this.Position + deltaPos;
		vector = deltaRot * (vector - transform.position) + transform.position;
		this.Position = vector;
		if (this.Hub.isLocalPlayer)
		{
			this.MainModule.MouseLook.CurrentHorizontal += deltaRot.eulerAngles.y;
		}
	}

	private bool TryGetOverride(out Vector3 overrideDir)
	{
		bool result = false;
		overrideDir = Vector3.zero;
		if (this.Hub.inventory.CurInstance.GetMobilityController() is IMovementInputOverride { MovementOverrideActive: not false } movementInputOverride)
		{
			result = true;
			overrideDir = movementInputOverride.MovementOverrideDirection;
		}
		for (int i = 0; i < this.Hub.playerEffectsController.EffectsLength; i++)
		{
			if (this.Hub.playerEffectsController.AllEffects[i] is IMovementInputOverride { MovementOverrideActive: not false } movementInputOverride2)
			{
				result = true;
				overrideDir += movementInputOverride2.MovementOverrideDirection;
			}
		}
		return result;
	}

	private static void ReloadInputConfigs()
	{
	}

	public void PopulateDummyActions(Action<DummyAction> actionAdder)
	{
		float[] array = new float[4] { 0.05f, 0.2f, 0.5f, 1.5f };
		foreach (float dist in array)
		{
			actionAdder(new DummyAction(string.Format("Walk {0} {1}m", "left", dist), delegate
			{
				SetPosition(Vector3.left, dist);
			}));
			actionAdder(new DummyAction(string.Format("Walk {0} {1}m", "right", dist), delegate
			{
				SetPosition(Vector3.right, dist);
			}));
			actionAdder(new DummyAction(string.Format("Walk {0} {1}m", "forward", dist), delegate
			{
				SetPosition(Vector3.forward, dist);
			}));
			actionAdder(new DummyAction(string.Format("Walk {0} {1}m", "back", dist), delegate
			{
				SetPosition(Vector3.back, dist);
			}));
		}
		void SetPosition(Vector3 dir, float distance)
		{
			Vector3 vector = this.Hub.PlayerCameraReference.TransformDirection(dir).NormalizeIgnoreY();
			this.ReceivedPosition = new RelativePosition(this.Position + vector * distance);
		}
	}
}
