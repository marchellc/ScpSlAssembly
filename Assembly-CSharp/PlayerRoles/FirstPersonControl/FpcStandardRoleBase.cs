using CustomPlayerEffects;
using GameObjectPools;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.Ragdolls;
using PlayerRoles.SpawnData;
using PlayerRoles.Spectating;
using PlayerRoles.Visibility;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl;

public abstract class FpcStandardRoleBase : PlayerRoleBase, ISpawnDataReader, IPrivateSpawnDataWriter, IPublicSpawnDataWriter, IHealthbarRole, IRagdollRole, IFpcRole, IPoolSpawnable, ICameraController, IAvatarRole, ISpectatableRole, IVoiceRole, ICustomVisibilityRole, IViewmodelRole, IAmbientLightRole, IAFKRole
{
	private Vector3 _lastPos;

	private const float PositionalTolerance = 1.25f;

	private Transform _hubTransform;

	private Transform _cameraTransform;

	private InsufficientLighting _noLightFx;

	public virtual Vector3 CameraPosition => this._cameraTransform.position;

	public virtual float VerticalRotation => this._cameraTransform.eulerAngles.x;

	public virtual float HorizontalRotation => this._cameraTransform.eulerAngles.y;

	public virtual PlayerStats TargetStats
	{
		get
		{
			if (!base.TryGetOwner(out var hub))
			{
				return null;
			}
			return hub.playerStats;
		}
	}

	public abstract float MaxHealth { get; }

	public abstract ISpawnpointHandler SpawnpointHandler { get; }

	public virtual float AmbientBoost
	{
		get
		{
			if (!this.ForceBlackAmbient)
			{
				return InsufficientLighting.DefaultIntensity;
			}
			return 0f;
		}
	}

	public virtual bool ForceBlackAmbient
	{
		get
		{
			if (!this.InsufficientLight)
			{
				return this.InDarkness;
			}
			return true;
		}
	}

	public virtual bool InsufficientLight
	{
		get
		{
			if (!NetworkServer.active)
			{
				return this._noLightFx.IsEnabled;
			}
			if (this.InDarkness)
			{
				return !this.HasFlashlight;
			}
			return false;
		}
	}

	[field: SerializeField]
	public FirstPersonMovementModule FpcModule { get; private set; }

	[field: SerializeField]
	public SpectatableModuleBase SpectatorModule { get; private set; }

	[field: SerializeField]
	public VoiceModuleBase VoiceModule { get; private set; }

	[field: SerializeField]
	public VisibilityController VisibilityController { get; private set; }

	[field: SerializeField]
	public BasicRagdoll Ragdoll { get; private set; }

	[field: SerializeField]
	public Texture RoleAvatar { get; private set; }

	private bool InDarkness => RoomLightController.IsInDarkenedRoom(this.FpcModule.Position);

	private bool HasFlashlight
	{
		get
		{
			if (!base.TryGetOwner(out var hub))
			{
				return false;
			}
			if (hub.inventory.CurInstance is ILightEmittingItem lightEmittingItem && lightEmittingItem != null)
			{
				return lightEmittingItem.IsEmittingLight;
			}
			return false;
		}
	}

	public bool IsAFK
	{
		get
		{
			if (this._lastPos == Vector3.zero)
			{
				this._lastPos = this._hubTransform.position;
				return true;
			}
			if ((this._lastPos - this._hubTransform.position).sqrMagnitude < 1.25f)
			{
				return true;
			}
			this._lastPos = this._hubTransform.position;
			return false;
		}
	}

	protected virtual void ShowStartScreen()
	{
		if (base.IsLocalPlayer)
		{
			StartScreen.Show(this);
		}
	}

	internal override void Init(ReferenceHub hub, RoleChangeReason spawnReason, RoleSpawnFlags spawnFlags)
	{
		base.Init(hub, spawnReason, spawnFlags);
		this._noLightFx = hub.playerEffectsController.GetEffect<InsufficientLighting>();
		this._lastPos = Vector3.zero;
	}

	public virtual void ReadSpawnData(NetworkReader reader)
	{
		RelativePosition receivedPosition = reader.ReadRelativePosition();
		this.FpcModule.MouseLook.ApplySyncValues(reader.ReadUShort(), 32767);
		if (receivedPosition.WaypointId == 0)
		{
			return;
		}
		if (!base.IsLocalPlayer)
		{
			FpcMotor motor = this.FpcModule.Motor;
			if (motor.ReceivedPosition.WaypointId != 0)
			{
				return;
			}
			motor.ReceivedPosition = receivedPosition;
		}
		this.FpcModule.Position = receivedPosition.Position;
	}

	public virtual void WritePublicSpawnData(NetworkWriter writer)
	{
		this.FpcModule.MouseLook.GetSyncValues(0, out var syncH, out var _);
		writer.WriteRelativePosition(new RelativePosition(this._hubTransform.position));
		writer.WriteUShort(syncH);
	}

	public virtual void WritePrivateSpawnData(NetworkWriter writer)
	{
	}

	public virtual void SpawnObject()
	{
		if (base.TryGetOwner(out var hub))
		{
			this._hubTransform = hub.transform;
			this._cameraTransform = hub.PlayerCameraReference;
			this.ShowStartScreen();
		}
	}

	public virtual bool TryGetViewmodelFov(out float fov)
	{
		fov = 0f;
		if (!base.TryGetOwner(out var hub))
		{
			return false;
		}
		ItemBase curInstance = hub.inventory.CurInstance;
		if (curInstance == null || curInstance.ViewModel == null)
		{
			return false;
		}
		fov = curInstance.ViewModel.ViewmodelCameraFOV;
		return true;
	}
}
