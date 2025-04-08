using System;
using CursorManagement;
using GameObjectPools;
using Interactables.Interobjects;
using Mirror;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.FirstPersonControl.Thirdperson;
using RelativePositioning;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerRoles.FirstPersonControl
{
	public class FirstPersonMovementModule : MonoBehaviour, IPoolSpawnable, IPoolResettable, ICursorOverride
	{
		public static event Action OnPositionUpdated;

		public virtual CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.Centered;
			}
		}

		public virtual bool LockMovement
		{
			get
			{
				return false;
			}
		}

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

		public float MaxMovementSpeed
		{
			get
			{
				return this.VelocityForState(this.ValidateMovementState(this._speedState), true);
			}
		}

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
				return (this.CharControllerSet ? this.CharController.isGrounded : this._syncGrounded) && !this.Noclip.IsActive;
			}
			set
			{
				if (this._syncGrounded == value)
				{
					return;
				}
				this._syncGrounded = value;
				if (value)
				{
					Action onGrounded = this.OnGrounded;
					if (onGrounded == null)
					{
						return;
					}
					onGrounded();
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

		private protected ReferenceHub Hub { protected get; private set; }

		private protected PlayerRoleBase Role { protected get; private set; }

		protected virtual FpcMotor NewMotor
		{
			get
			{
				return new FpcMotor(this.Hub, this, !this.Hub.IsSCP(true) && !this.Hub.IsFlamingo(true));
			}
		}

		protected virtual FpcNoclip NewNoclip
		{
			get
			{
				return new FpcNoclip(this.Hub, this);
			}
		}

		protected virtual FpcMouseLook NewMouseLook
		{
			get
			{
				return new FpcMouseLook(this.Hub, this);
			}
		}

		protected virtual FpcStateProcessor NewStateProcessor
		{
			get
			{
				return new FpcStateProcessor(this.Hub, this);
			}
		}

		protected virtual void UpdateMovement()
		{
			this.SyncMovementState = this.StateProcessor.UpdateMovementState(this.CurrentMovementState);
			bool flag;
			this.Motor.UpdatePosition(out flag);
			this.Noclip.UpdateNoclip();
			this.MouseLook.UpdateRotation();
			if (this.SyncMovementState != PlayerMovementState.Crouching)
			{
				this._speedState = this.SyncMovementState;
			}
			if (this.Hub.isLocalPlayer)
			{
				float num = this.VelocityForState(PlayerMovementState.Walking, false);
				PlayerMovementState playerMovementState;
				this.StateProcessor.ClientUpdateInput(this, num, out playerMovementState);
				this.Motor.ReceivedPosition = new RelativePosition(this.Position);
				NetworkClient.Send<FpcFromClientMessage>(new FpcFromClientMessage(this.Motor.ReceivedPosition, playerMovementState, flag, this.MouseLook), 0);
			}
			this.CharacterModelInstance.OnPlayerMove();
		}

		private void FixedUpdate()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.Tracer.Record(this.Position);
		}

		private void OnRoleDisabled(RoleTypeId rid)
		{
			this.CharControllerSet = false;
			this.CharacterModelInstance.ReturnToPool(true);
			this.Noclip.ShutdownModule();
		}

		protected virtual PlayerMovementState ValidateMovementState(PlayerMovementState state)
		{
			switch (state)
			{
			case PlayerMovementState.Crouching:
				if (this.CrouchSpeed != 0f)
				{
					return state;
				}
				break;
			case PlayerMovementState.Sneaking:
				if (this.SneakSpeed != 0f)
				{
					return state;
				}
				break;
			case PlayerMovementState.Walking:
				return state;
			case PlayerMovementState.Sprinting:
				if (this.SprintSpeed != 0f)
				{
					return state;
				}
				break;
			default:
				return state;
			}
			return PlayerMovementState.Walking;
		}

		public void ServerOverridePosition(Vector3 position)
		{
			this.Position = position;
			this.Hub.connectionToClient.Send<FpcPositionOverrideMessage>(new FpcPositionOverrideMessage(position), 0);
			this.OnServerPositionOverwritten();
		}

		public void ServerOverrideRotation(Vector2 rotation)
		{
			this.MouseLook.CurrentVertical = rotation.x;
			this.MouseLook.CurrentHorizontal = rotation.y;
			this.Hub.connectionToClient.Send<FpcRotationOverrideMessage>(new FpcRotationOverrideMessage(rotation), 0);
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
			case PlayerMovementState.Walking:
				num = this.WalkSpeed;
				break;
			case PlayerMovementState.Sprinting:
				num = this.SprintSpeed;
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
				IMovementSpeedModifier movementSpeedModifier = this.Hub.playerEffectsController.AllEffects[i] as IMovementSpeedModifier;
				if (movementSpeedModifier != null && movementSpeedModifier.MovementModifierActive)
				{
					num2 = Mathf.Min(num2, movementSpeedModifier.MovementSpeedLimit);
					num *= movementSpeedModifier.MovementSpeedMultiplier;
				}
			}
			return Mathf.Min(num, num2);
		}

		public virtual void SpawnObject()
		{
			PlayerRoleBase playerRoleBase;
			ReferenceHub referenceHub;
			if (!base.TryGetComponent<PlayerRoleBase>(out playerRoleBase) || !playerRoleBase.TryGetOwner(out referenceHub))
			{
				throw new InvalidOperationException("Movement module failed to initiate. Unable to find owner of the role.");
			}
			FirstPersonMovementModule._activeUpdates = (Action)Delegate.Combine(FirstPersonMovementModule._activeUpdates, new Action(this.UpdateMovement));
			this.Hub = referenceHub;
			this.Role = playerRoleBase;
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
			role.OnRoleDisabled = (Action<RoleTypeId>)Delegate.Combine(role.OnRoleDisabled, new Action<RoleTypeId>(this.OnRoleDisabled));
			ElevatorChamber.OnElevatorMoved += this.Motor.OnElevatorMoved;
			this.SetModel(this.CharacterModelTemplate);
			this.ModuleReady = true;
		}

		protected virtual void SetModel(GameObject template)
		{
			PoolObject poolObject;
			if (PoolManager.Singleton.TryGetPoolObject(template, null, out poolObject, true))
			{
				CharacterModel characterModel = poolObject as CharacterModel;
				if (characterModel != null)
				{
					SceneManager.MoveGameObjectToScene(poolObject.gameObject, SceneManager.GetActiveScene());
					this.CharacterModelInstance = characterModel;
					Transform transform = template.transform;
					characterModel.Setup(this.Hub, this.Role as IFpcRole, transform.localPosition, transform.localRotation);
					characterModel.transform.SetParent(this.Hub.transform, false);
					return;
				}
			}
			Debug.LogError("Can't spawn '" + template.name + "' - FPC models must derive from CharacterModel.");
		}

		public virtual void ResetObject()
		{
			CursorManager.Unregister(this);
			FirstPersonMovementModule._activeUpdates = (Action)Delegate.Remove(FirstPersonMovementModule._activeUpdates, new Action(this.UpdateMovement));
			PlayerRoleBase role = this.Role;
			role.OnRoleDisabled = (Action<RoleTypeId>)Delegate.Remove(role.OnRoleDisabled, new Action<RoleTypeId>(this.OnRoleDisabled));
			ElevatorChamber.OnElevatorMoved -= this.Motor.OnElevatorMoved;
			this.ModuleReady = false;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StaticUnityMethods.OnUpdate += delegate
			{
				if (FirstPersonMovementModule._activeUpdates == null)
				{
					return;
				}
				FirstPersonMovementModule._movementUpdateCycle = true;
				FirstPersonMovementModule._activeUpdates();
				FirstPersonMovementModule._movementUpdateCycle = false;
				Action onPositionUpdated = FirstPersonMovementModule.OnPositionUpdated;
				if (onPositionUpdated == null)
				{
					return;
				}
				onPositionUpdated();
			};
		}

		public Action OnServerPositionOverwritten;

		public Action OnGrounded;

		public GameObject CharacterModelTemplate;

		public float CrouchSpeed;

		public float SneakSpeed;

		public float WalkSpeed;

		public float SprintSpeed;

		public float JumpSpeed;

		public CharacterControllerSettingsPreset CharacterControllerSettings;

		public float CrouchHeightRatio;

		private Transform _transform;

		private PlayerMovementState _speedState;

		private bool _syncGrounded;

		private Vector3 _cachedPosition;

		private static bool _movementUpdateCycle;

		private static Action _activeUpdates;
	}
}
