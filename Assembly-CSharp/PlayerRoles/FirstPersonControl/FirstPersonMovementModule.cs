using System;
using CursorManagement;
using GameObjectPools;
using Interactables.Interobjects;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.FirstPersonControl.Thirdperson;
using RelativePositioning;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerRoles.FirstPersonControl;

public class FirstPersonMovementModule : MonoBehaviour, IPoolSpawnable, IPoolResettable, ICursorOverride, IRootDummyActionProvider
{
	public Action OnServerPositionOverwritten;

	public Action OnGrounded;

	public GameObject CharacterModelTemplate;

	public float CrouchSpeed;

	public float SneakSpeed;

	public float WalkSpeed;

	public float SprintSpeed;

	public float JumpSpeed;

	public CharacterControllerSettingsPreset CharacterControllerSettings;

	public FallDamageSettings FallDamageSettings;

	public float CrouchHeightRatio;

	private Transform _transform;

	private PlayerMovementState _speedState;

	private bool _syncGrounded;

	private Vector3 _cachedPosition;

	private static bool _movementUpdateCycle;

	private static Action _activeUpdates;

	public virtual CursorOverrideMode CursorOverride => CursorOverrideMode.Centered;

	public virtual bool LockMovement => false;

	public CharacterController CharController { get; private set; }

	public bool CharControllerSet { get; private set; }

	public bool ModuleReady { get; private set; }

	public FpcMotor Motor { get; protected set; }

	public FpcNoclip Noclip { get; protected set; }

	public FpcMouseLook MouseLook { get; protected set; }

	public FpcStateProcessor StateProcessor { get; protected set; }

	public MovementTracer Tracer { get; private set; }

	public CharacterModel CharacterModelInstance { get; protected set; }

	public FpcSyncData LastSentData { get; internal set; }

	public float MaxMovementSpeed => this.VelocityForState(this.ValidateMovementState(this._speedState), applyCrouch: true);

	public PlayerMovementState CurrentMovementState
	{
		get
		{
			return this.ValidateMovementState(this.SyncMovementState);
		}
		set
		{
			this.SyncMovementState = value;
		}
	}

	public PlayerMovementState SyncMovementState { get; private set; }

	public bool IsGrounded
	{
		get
		{
			if (this.CharControllerSet ? this.CharController.isGrounded : this._syncGrounded)
			{
				return !this.Noclip.IsActive;
			}
			return false;
		}
		set
		{
			if (this._syncGrounded != value)
			{
				this._syncGrounded = value;
				if (value)
				{
					this.OnGrounded?.Invoke();
				}
			}
		}
	}

	public Vector3 Position
	{
		get
		{
			return this._cachedPosition;
		}
		set
		{
			this._transform.position = value;
			this._cachedPosition = value;
			if (!FirstPersonMovementModule._movementUpdateCycle && this.ModuleReady)
			{
				this.CharacterModelInstance.OnPlayerMove();
			}
		}
	}

	protected ReferenceHub Hub { get; private set; }

	protected PlayerRoleBase Role { get; private set; }

	protected virtual FpcMotor NewMotor => new FpcMotor(this.Hub, this, this.FallDamageSettings);

	protected virtual FpcNoclip NewNoclip => new FpcNoclip(this.Hub, this);

	protected virtual FpcMouseLook NewMouseLook => new FpcMouseLook(this.Hub, this);

	protected virtual FpcStateProcessor NewStateProcessor => new FpcStateProcessor(this.Hub, this);

	public bool DummyActionsDirty { get; set; }

	public static event Action OnPositionUpdated;

	protected virtual void UpdateMovement()
	{
		this.SyncMovementState = this.StateProcessor.UpdateMovementState(this.CurrentMovementState);
		this.Motor.UpdatePosition(out var sendJump);
		this.Noclip.UpdateNoclip();
		this.MouseLook.UpdateRotation();
		if (this.SyncMovementState != PlayerMovementState.Crouching)
		{
			this._speedState = this.SyncMovementState;
		}
		if (this.Hub.isLocalPlayer)
		{
			float walkSpeed = this.VelocityForState(PlayerMovementState.Walking, applyCrouch: false);
			this.StateProcessor.ClientUpdateInput(this, walkSpeed, out var valueToSend);
			this.Motor.ReceivedPosition = new RelativePosition(this.Position);
			NetworkClient.Send(new FpcFromClientMessage(this.Motor.ReceivedPosition, valueToSend, sendJump, this.MouseLook));
		}
		this.CharacterModelInstance.OnPlayerMove();
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			this.Tracer.Record(this.Position);
		}
	}

	private void OnRoleDisabled(RoleTypeId rid)
	{
		this.CharControllerSet = false;
		this.CharacterModelInstance.ReturnToPool();
		this.Noclip.ShutdownModule();
	}

	protected virtual PlayerMovementState ValidateMovementState(PlayerMovementState state)
	{
		switch (state)
		{
		case PlayerMovementState.Crouching:
			if (this.CrouchSpeed != 0f)
			{
				break;
			}
			goto IL_0045;
		case PlayerMovementState.Sneaking:
			if (this.SneakSpeed != 0f)
			{
				break;
			}
			goto IL_0045;
		case PlayerMovementState.Sprinting:
			{
				if (this.SprintSpeed != 0f)
				{
					break;
				}
				goto IL_0045;
			}
			IL_0045:
			return PlayerMovementState.Walking;
		}
		return state;
	}

	public void ServerOverridePosition(Vector3 position)
	{
		this.Position = position;
		this.Hub.connectionToClient.Send(new FpcPositionOverrideMessage(position));
		this.OnServerPositionOverwritten();
	}

	public void ServerOverrideRotation(Vector2 rotation)
	{
		this.MouseLook.CurrentVertical = rotation.x;
		this.MouseLook.CurrentHorizontal = rotation.y;
		this.Hub.connectionToClient.Send(new FpcRotationOverrideMessage(rotation));
	}

	public virtual float VelocityForState(PlayerMovementState state, bool applyCrouch)
	{
		float num = 0f;
		switch (state)
		{
		case PlayerMovementState.Crouching:
			num = this.CrouchSpeed;
			break;
		case PlayerMovementState.Sneaking:
			num = this.SneakSpeed;
			break;
		case PlayerMovementState.Sprinting:
			num = this.SprintSpeed;
			break;
		case PlayerMovementState.Walking:
			num = this.WalkSpeed;
			break;
		}
		if (applyCrouch)
		{
			num = Mathf.Lerp(num, this.CrouchSpeed, this.StateProcessor.CrouchPercent);
		}
		num *= this.Hub.inventory.MovementSpeedMultiplier;
		float num2 = this.Hub.inventory.MovementSpeedLimit;
		for (int i = 0; i < this.Hub.playerEffectsController.EffectsLength; i++)
		{
			if (this.Hub.playerEffectsController.AllEffects[i] is IMovementSpeedModifier { MovementModifierActive: not false } movementSpeedModifier)
			{
				num2 = Mathf.Min(num2, movementSpeedModifier.MovementSpeedLimit);
				num *= movementSpeedModifier.MovementSpeedMultiplier;
			}
		}
		return Mathf.Min(num, num2);
	}

	public virtual void SpawnObject()
	{
		if (!base.TryGetComponent<PlayerRoleBase>(out var component) || !component.TryGetOwner(out var hub))
		{
			throw new InvalidOperationException("Movement module failed to initiate. Unable to find owner of the role.");
		}
		FirstPersonMovementModule._activeUpdates = (Action)Delegate.Combine(FirstPersonMovementModule._activeUpdates, new Action(UpdateMovement));
		this.Hub = hub;
		this.Role = component;
		this._transform = this.Hub.transform;
		this._cachedPosition = base.transform.position;
		this._speedState = PlayerMovementState.Walking;
		this.SyncMovementState = PlayerMovementState.Walking;
		if (NetworkServer.active || this.Hub.isLocalPlayer)
		{
			this.CharController = this.Hub.GetComponent<CharacterController>();
			this.CharacterControllerSettings.Apply(this.CharController);
			this.CharControllerSet = true;
			if (NetworkServer.active)
			{
				this.Tracer = new MovementTracer(15, 3, 50f);
			}
			if (this.Hub.isLocalPlayer)
			{
				CursorManager.Register(this);
			}
		}
		else
		{
			this.CharControllerSet = false;
		}
		this.Motor = this.NewMotor;
		this.Noclip = this.NewNoclip;
		this.MouseLook = this.NewMouseLook;
		this.StateProcessor = this.NewStateProcessor;
		PlayerRoleBase role = this.Role;
		role.OnRoleDisabled = (Action<RoleTypeId>)Delegate.Combine(role.OnRoleDisabled, new Action<RoleTypeId>(OnRoleDisabled));
		ElevatorChamber.OnElevatorMoved += this.Motor.OnElevatorMoved;
		this.SetModel(this.CharacterModelTemplate);
		this.ModuleReady = true;
	}

	protected virtual void SetModel(GameObject template)
	{
		if (PoolManager.Singleton.TryGetPoolObject(template, null, out var poolObject) && poolObject is CharacterModel characterModel)
		{
			SceneManager.MoveGameObjectToScene(poolObject.gameObject, SceneManager.GetActiveScene());
			this.CharacterModelInstance = characterModel;
			Transform transform = template.transform;
			characterModel.Setup(this.Hub, this.Role as IFpcRole, transform.localPosition, transform.localRotation);
			characterModel.transform.SetParent(this.Hub.transform, worldPositionStays: false);
		}
		else
		{
			Debug.LogError("Can't spawn '" + template.name + "' - FPC models must derive from CharacterModel.");
		}
	}

	public virtual void ResetObject()
	{
		CursorManager.Unregister(this);
		FirstPersonMovementModule._activeUpdates = (Action)Delegate.Remove(FirstPersonMovementModule._activeUpdates, new Action(UpdateMovement));
		PlayerRoleBase role = this.Role;
		role.OnRoleDisabled = (Action<RoleTypeId>)Delegate.Remove(role.OnRoleDisabled, new Action<RoleTypeId>(OnRoleDisabled));
		ElevatorChamber.OnElevatorMoved -= this.Motor.OnElevatorMoved;
		this.ModuleReady = false;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnUpdate += delegate
		{
			if (FirstPersonMovementModule._activeUpdates != null)
			{
				FirstPersonMovementModule._movementUpdateCycle = true;
				FirstPersonMovementModule._activeUpdates();
				FirstPersonMovementModule._movementUpdateCycle = false;
				FirstPersonMovementModule.OnPositionUpdated?.Invoke();
			}
		};
	}

	public void PopulateDummyActions(Action<DummyAction> actionAdder, Action<string> categoryAdder)
	{
		this.DummyActionsDirty = false;
		categoryAdder("MouseLook");
		this.MouseLook.PopulateDummyActions(actionAdder);
		categoryAdder("Motor");
		this.Motor.PopulateDummyActions(actionAdder);
	}
}
