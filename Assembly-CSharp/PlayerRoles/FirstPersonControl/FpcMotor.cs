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
			if (!_requestedJump)
			{
				if (Hub.isLocalPlayer && Input.GetKeyDown(_keyJump))
				{
					return !InputLocked;
				}
				return false;
			}
			return true;
		}
		set
		{
			_requestedJump = value;
		}
	}

	protected virtual float Speed => MainModule.MaxMovementSpeed;

	protected virtual Vector3 DesiredMove
	{
		get
		{
			if (ViewMode == FpcViewMode.LocalPlayer)
			{
				if (TryGetOverride(out var overrideDir))
				{
					return overrideDir;
				}
				if (InputLocked)
				{
					return Vector3.zero;
				}
				float num = 0f;
				float num2 = 0f;
				if (Input.GetKey(_keyFwd))
				{
					num2 += 1f;
				}
				if (Input.GetKey(_keyBwd))
				{
					num2 -= 1f;
				}
				if (Input.GetKey(_keyRgt))
				{
					num += 1f;
				}
				if (Input.GetKey(_keyLft))
				{
					num -= 1f;
				}
				return CachedTransform.forward * num2 + CachedTransform.right * num;
			}
			Vector3 position = ReceivedPosition.Position;
			Vector3 vector = position - Position;
			if (NetworkServer.active)
			{
				float num3 = Mathf.Clamp(vector.y * 1.6f, 0f, 0.35f * MainModule.JumpSpeed);
				MainModule.CharController.stepOffset = Mathf.Min(_defaultStepOffset + num3, _defaultHeight);
				if (MainModule.Noclip.RecentlyActive)
				{
					Position = position;
					return Vector3.zero;
				}
			}
			float num4 = vector.MagnitudeIgnoreY();
			if (num4 < 0.03f)
			{
				return Vector3.zero;
			}
			num4 -= 0.5f;
			if (num4 < _lastMaxSpeed && Mathf.Abs(vector.y) < Mathf.Max(MainModule.JumpSpeed, Mathf.Abs(MoveDirection.y)))
			{
				if (!NetworkServer.active)
				{
					return vector;
				}
				return new Vector3(vector.x, 0f, vector.z);
			}
			if (NetworkServer.active)
			{
				if (_lastOverrideTime.Elapsed.TotalSeconds > 0.4000000059604645)
				{
					MainModule.ServerOverridePosition(Position);
				}
			}
			else
			{
				Position = position;
			}
			return Vector3.zero;
		}
	}

	private Vector3 Position
	{
		get
		{
			return MainModule.Position;
		}
		set
		{
			MainModule.Position = value;
		}
	}

	private bool InputLocked => CursorManager.MovementLocked;

	public event Action<Vector3> OnBeforeMove;

	public FpcMotor(ReferenceHub hub, FirstPersonMovementModule module, FallDamageSettings fallDamageSettings)
	{
		Hub = hub;
		MainModule = module;
		GravityController = new FpcGravityController(this);
		_fallDamageSettings = fallDamageSettings;
		CachedTransform = hub.transform;
		if (NetworkServer.active)
		{
			_lastOverrideTime = Stopwatch.StartNew();
			_fallDamageImmunity = Stopwatch.StartNew();
			module.OnServerPositionOverwritten = (Action)Delegate.Combine(module.OnServerPositionOverwritten, (Action)delegate
			{
				_lastOverrideTime.Restart();
			});
			_defaultStepOffset = module.CharController.stepOffset;
			_defaultHeight = module.CharController.height;
		}
		if (Hub.isLocalPlayer)
		{
			ViewMode = FpcViewMode.LocalPlayer;
			ReloadInputConfigs();
			if (!_reloadKeysEventSet)
			{
				NewInput.OnAnyModified += ReloadInputConfigs;
				_reloadKeysEventSet = true;
			}
		}
		else
		{
			ViewMode = (NetworkServer.active ? FpcViewMode.Server : FpcViewMode.Thirdperson);
		}
	}

	public void UpdatePosition(out bool sendJump)
	{
		sendJump = false;
		bool flag = Speed < 0f;
		_lastMaxSpeed = Mathf.Abs(Speed);
		if (MainModule.Noclip.IsActive)
		{
			MoveDirection = Vector3.zero;
			return;
		}
		if (ViewMode == FpcViewMode.Thirdperson)
		{
			UpdateThirdperson();
			return;
		}
		CharacterController charController = MainModule.CharController;
		Physics.SphereCast(Position, charController.radius, Vector3.down, out var hitInfo, charController.height / 2f, FpcStateProcessor.Mask, QueryTriggerInteraction.Ignore);
		Vector3 vector = Vector3.ProjectOnPlane(DesiredMove, hitInfo.normal).normalized;
		if (ViewMode == FpcViewMode.LocalPlayer && flag)
		{
			vector = -vector;
		}
		MoveDirection = new Vector3(vector.x * _lastMaxSpeed, MoveDirection.y, vector.z * _lastMaxSpeed);
		if (charController.isGrounded)
		{
			UpdateGrounded(ref sendJump, MainModule.JumpSpeed);
		}
		else
		{
			UpdateFloating();
		}
	}

	public void ResetFallDamageCooldown()
	{
		_fallDamageImmunity.Restart();
	}

	protected virtual Vector3 GetFrameMove()
	{
		if (MainModule.Noclip.IsActive)
		{
			return Vector3.zero;
		}
		Vector3 result = MoveDirection * Time.deltaTime;
		if (ViewMode != 0)
		{
			Vector3 position = ReceivedPosition.Position;
			Vector3 position2 = Position;
			result.x = ClampMoveDirection(position2.x, position.x, result.x);
			result.z = ClampMoveDirection(position2.z, position.z, result.z);
		}
		return result;
	}

	protected virtual void Move()
	{
		CharacterController charController = MainModule.CharController;
		Vector3 position = Position;
		Vector3 frameMove = GetFrameMove();
		this.OnBeforeMove?.Invoke(frameMove);
		charController.Move(frameMove);
		Position = CachedTransform.position;
		MovementDetected = Position != position;
		Velocity = (Position - position) / Time.deltaTime;
		MainModule.IsGrounded = charController.isGrounded;
	}

	protected virtual void UpdateGrounded(ref bool sendJump, float jumpSpeed)
	{
		Vector3 moveDirection = MoveDirection;
		bool flag = false;
		if (WantsToJump)
		{
			if (jumpSpeed > 0f)
			{
				moveDirection.y = jumpSpeed;
				flag = true;
			}
			_requestedJump = false;
			IsJumping = true;
			sendJump = true;
		}
		else
		{
			moveDirection.y = -10f;
			IsJumping = false;
		}
		MoveDirection = moveDirection;
		if (_fallDamageSettings.Enabled && _maxFallSpeed > _fallDamageSettings.MinVelocity)
		{
			ServerProcessFall(_maxFallSpeed - _fallDamageSettings.MinVelocity);
		}
		_maxFallSpeed = _fallDamageSettings.MinVelocity;
		if (flag)
		{
			UpdateFloating();
			return;
		}
		Move();
		if (!MainModule.CharController.isGrounded)
		{
			MoveDirection = Vector3.Scale(MoveDirection, new Vector3(1f, 0f, 1f));
		}
	}

	protected virtual void UpdateFloating()
	{
		Vector3 vector = 0.5f * Time.deltaTime * GravityController.Gravity;
		MoveDirection += vector;
		Move();
		MoveDirection += vector;
		_maxFallSpeed = Mathf.Max(_maxFallSpeed, 0f - MoveDirection.y);
	}

	private static float ClampMoveDirection(float curPos, float targetPos, float moveDir)
	{
		float num = Mathf.Abs(curPos - targetPos);
		return Mathf.Clamp(moveDir, 0f - num, num);
	}

	private void UpdateThirdperson()
	{
		if (IsInvisible)
		{
			Position = InvisiblePosition;
			return;
		}
		Vector3 desiredMove = DesiredMove;
		Vector3 position = ReceivedPosition.Position;
		Vector3 position2 = Position;
		Vector3 b = new Vector3(position.x, position2.y, position.z);
		float num = Time.deltaTime * Mathf.Max(3f, _lastMaxSpeed);
		Vector3 vector = Vector3.Lerp(position2, b, num * 2f);
		vector.y = Mathf.Lerp(vector.y, position.y, 9f * Time.deltaTime);
		Position = vector;
		Velocity = (vector - position2) / Time.deltaTime;
		MoveDirection = new Vector3(desiredMove.x, 0f, desiredMove.z).normalized * _lastMaxSpeed + Vector3.up * desiredMove.y;
	}

	private void ServerProcessFall(float speed)
	{
		if (NetworkServer.active && !(_fallDamageImmunity.Elapsed.TotalSeconds < (double)_fallDamageSettings.ImmunityTime))
		{
			PlayerRoleBase currentRole = Hub.roleManager.CurrentRole;
			if (!(currentRole is Scp106Role { IsStalking: not false }))
			{
				RoleTypeId roleTypeId = currentRole.RoleTypeId;
				Vector3 position = Position;
				float damage = _fallDamageSettings.CalculateDamage(speed);
				Hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Falldown));
				new FpcFallDamageMessage(Hub, position, roleTypeId).SendToAuthenticated();
			}
		}
	}

	public void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamb, Vector3 deltaPos, Quaternion deltaRot)
	{
		if (!elevatorBounds.Contains(Position))
		{
			return;
		}
		if (NetworkServer.active)
		{
			Vector3 position = ReceivedPosition.Position;
			Vector3 point = new Vector3(position.x, Position.y, position.z);
			if (elevatorBounds.Contains(point))
			{
				_lastOverrideTime.Restart();
			}
		}
		Transform transform = chamb.transform;
		Vector3 vector = Position + deltaPos;
		vector = deltaRot * (vector - transform.position) + transform.position;
		Position = vector;
		if (Hub.isLocalPlayer)
		{
			MainModule.MouseLook.CurrentHorizontal += deltaRot.eulerAngles.y;
		}
	}

	private bool TryGetOverride(out Vector3 overrideDir)
	{
		bool result = false;
		overrideDir = Vector3.zero;
		if (Hub.inventory.CurInstance.GetMobilityController() is IMovementInputOverride { MovementOverrideActive: not false } movementInputOverride)
		{
			result = true;
			overrideDir = movementInputOverride.MovementOverrideDirection;
		}
		for (int i = 0; i < Hub.playerEffectsController.EffectsLength; i++)
		{
			if (Hub.playerEffectsController.AllEffects[i] is IMovementInputOverride { MovementOverrideActive: not false } movementInputOverride2)
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
			Vector3 vector = Hub.PlayerCameraReference.TransformDirection(dir).NormalizeIgnoreY();
			ReceivedPosition = new RelativePosition(Position + vector * distance);
		}
	}
}
