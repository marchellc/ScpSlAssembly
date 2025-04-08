using System;
using System.Collections.Generic;
using GameObjectPools;
using MapGeneration;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using PlayerRoles.Voice;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079Role : PlayerRoleBase, ISubroutinedRole, ISpectatableRole, ISpawnableScp, ITeslaControllerRole, IAdvancedCameraController, ICameraController, IPoolResettable, IPoolSpawnable, IVoiceRole, IAvatarRole, IHudScp, IAmbientLightRole, IAFKRole
	{
		public static Scp079Role LocalInstance { get; private set; }

		public static bool LocalInstanceActive { get; private set; }

		public ScpHudBase HudPrefab { get; private set; }

		public SubroutineManagerModule SubroutineModule { get; private set; }

		public VoiceModuleBase VoiceModule { get; private set; }

		public Texture RoleAvatar { get; private set; }

		public SpectatableModuleBase SpectatorModule { get; private set; }

		public override RoleTypeId RoleTypeId
		{
			get
			{
				return RoleTypeId.Scp079;
			}
		}

		public override Team Team
		{
			get
			{
				return Team.SCPs;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return Color.red;
			}
		}

		public Vector3 CameraPosition
		{
			get
			{
				return this.CurrentCamera.CameraPosition;
			}
		}

		public float VerticalRotation
		{
			get
			{
				return this.CurrentCamera.VerticalRotation;
			}
		}

		public float HorizontalRotation
		{
			get
			{
				return this.CurrentCamera.HorizontalRotation;
			}
		}

		public float RollRotation
		{
			get
			{
				return this.CurrentCamera.RollRotation;
			}
		}

		public Scp079Camera CurrentCamera
		{
			get
			{
				return this._curCamSync.CurrentCamera;
			}
		}

		public bool IsSpectated
		{
			get
			{
				ReferenceHub referenceHub;
				return SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub) && referenceHub.roleManager.CurrentRole == this;
			}
		}

		public float AmbientBoost
		{
			get
			{
				return 0f;
			}
		}

		public bool ForceBlackAmbient
		{
			get
			{
				return false;
			}
		}

		public bool InsufficientLight
		{
			get
			{
				return false;
			}
		}

		public bool IsAFK
		{
			get
			{
				if (this._lastCamPos == Vector3.zero)
				{
					this._lastCamPos = this.CurrentCamera.CameraPosition;
				}
				Vector3 cameraPosition = this.CurrentCamera.CameraPosition;
				if (cameraPosition == this._lastCamPos)
				{
					return true;
				}
				this._lastCamPos = cameraPosition;
				return false;
			}
		}

		public bool CanActivateShock { get; }

		public float GetSpawnChance(List<RoleTypeId> alreadySpawned)
		{
			int count = alreadySpawned.Count;
			return (float)((count == 0 || alreadySpawned.Contains(RoleTypeId.Scp096)) ? 0 : count);
		}

		public bool IsInIdleRange(TeslaGate teslaGate)
		{
			Scp079CurrentCameraSync scp079CurrentCameraSync;
			Scp079Camera scp079Camera;
			return this.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out scp079CurrentCameraSync) && scp079CurrentCameraSync.TryGetCurrentCamera(out scp079Camera) && RoomUtils.IsTheSameRoom(scp079Camera.Position, teslaGate.Position);
		}

		public void ResetObject()
		{
			Scp079Role.ActiveInstances.Remove(this);
			this._lastCamPos = Vector3.zero;
		}

		public void SpawnObject()
		{
			Scp079Role.ActiveInstances.Add(this);
			ReferenceHub referenceHub;
			if (!base.TryGetOwner(out referenceHub))
			{
				throw new InvalidOperationException("SCP-079 role failed to spawn - owner is null");
			}
			float num = 6000f;
			referenceHub.transform.position = Vector3.up * num;
			this._lastCamPos = Vector3.zero;
		}

		private void Awake()
		{
			this.SubroutineModule.TryGetSubroutine<Scp079CurrentCameraSync>(out this._curCamSync);
		}

		private void OnDestroy()
		{
			Scp079Role.ActiveInstances.Remove(this);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub x, PlayerRoleBase y, PlayerRoleBase z)
			{
				if (!x.isLocalPlayer)
				{
					return;
				}
				Scp079Role scp079Role = z as Scp079Role;
				if (scp079Role != null)
				{
					Scp079Role.LocalInstance = scp079Role;
					Scp079Role.LocalInstanceActive = true;
					return;
				}
				Scp079Role.LocalInstanceActive = false;
			};
		}

		public static readonly HashSet<Scp079Role> ActiveInstances = new HashSet<Scp079Role>();

		private Scp079CurrentCameraSync _curCamSync;

		private Vector3 _lastCamPos;
	}
}
