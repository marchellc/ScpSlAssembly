using System;
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

namespace PlayerRoles.FirstPersonControl
{
	public abstract class FpcStandardRoleBase : PlayerRoleBase, ISpawnDataReader, IPrivateSpawnDataWriter, IPublicSpawnDataWriter, IHealthbarRole, IRagdollRole, IFpcRole, IPoolSpawnable, ICameraController, IAvatarRole, ISpectatableRole, IVoiceRole, ICustomVisibilityRole, IViewmodelRole, IAmbientLightRole, IAFKRole
	{
		public virtual Vector3 CameraPosition
		{
			get
			{
				return this._cameraTransform.position;
			}
		}

		public virtual float VerticalRotation
		{
			get
			{
				return this._cameraTransform.eulerAngles.x;
			}
		}

		public virtual float HorizontalRotation
		{
			get
			{
				return this._cameraTransform.eulerAngles.y;
			}
		}

		public virtual PlayerStats TargetStats
		{
			get
			{
				ReferenceHub referenceHub;
				if (!base.TryGetOwner(out referenceHub))
				{
					return null;
				}
				return referenceHub.playerStats;
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
				return this.InsufficientLight || this.InDarkness;
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
				return this.InDarkness && !this.HasFlashlight;
			}
		}

		public FirstPersonMovementModule FpcModule { get; private set; }

		public SpectatableModuleBase SpectatorModule { get; private set; }

		public VoiceModuleBase VoiceModule { get; private set; }

		public VisibilityController VisibilityController { get; private set; }

		public BasicRagdoll Ragdoll { get; private set; }

		public Texture RoleAvatar { get; private set; }

		private bool InDarkness
		{
			get
			{
				return RoomLightController.IsInDarkenedRoom(this.FpcModule.Position);
			}
		}

		private bool HasFlashlight
		{
			get
			{
				ReferenceHub referenceHub;
				if (!base.TryGetOwner(out referenceHub))
				{
					return false;
				}
				ILightEmittingItem lightEmittingItem = referenceHub.inventory.CurInstance as ILightEmittingItem;
				return lightEmittingItem != null && lightEmittingItem != null && lightEmittingItem.IsEmittingLight;
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
			if (!base.IsLocalPlayer)
			{
				return;
			}
			StartScreen.Show(this);
		}

		internal override void Init(ReferenceHub hub, RoleChangeReason spawnReason, RoleSpawnFlags spawnFlags)
		{
			base.Init(hub, spawnReason, spawnFlags);
			this._noLightFx = hub.playerEffectsController.GetEffect<InsufficientLighting>();
			this._lastPos = Vector3.zero;
		}

		public virtual void ReadSpawnData(NetworkReader reader)
		{
			RelativePosition relativePosition = reader.ReadRelativePosition();
			this.FpcModule.MouseLook.ApplySyncValues(reader.ReadUShort(), 32767);
			if (relativePosition.WaypointId == 0)
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
				motor.ReceivedPosition = relativePosition;
			}
			this.FpcModule.Position = relativePosition.Position;
		}

		public virtual void WritePublicSpawnData(NetworkWriter writer)
		{
			ushort num;
			ushort num2;
			this.FpcModule.MouseLook.GetSyncValues(0, out num, out num2);
			writer.WriteRelativePosition(new RelativePosition(this._hubTransform.position));
			writer.WriteUShort(num);
		}

		public virtual void WritePrivateSpawnData(NetworkWriter writer)
		{
		}

		public virtual void SpawnObject()
		{
			ReferenceHub referenceHub;
			if (!base.TryGetOwner(out referenceHub))
			{
				return;
			}
			this._hubTransform = referenceHub.transform;
			this._cameraTransform = referenceHub.PlayerCameraReference;
			this.ShowStartScreen();
		}

		public virtual bool TryGetViewmodelFov(out float fov)
		{
			fov = 0f;
			ReferenceHub referenceHub;
			if (!base.TryGetOwner(out referenceHub))
			{
				return false;
			}
			ItemBase curInstance = referenceHub.inventory.CurInstance;
			if (curInstance == null || curInstance.ViewModel == null)
			{
				return false;
			}
			fov = curInstance.ViewModel.ViewmodelCameraFOV;
			return true;
		}

		private Vector3 _lastPos;

		private const float PositionalTolerance = 1.25f;

		private Transform _hubTransform;

		private Transform _cameraTransform;

		private InsufficientLighting _noLightFx;
	}
}
