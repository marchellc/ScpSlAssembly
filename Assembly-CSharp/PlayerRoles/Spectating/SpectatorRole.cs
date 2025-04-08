using System;
using CentralAuth;
using CustomPlayerEffects;
using GameObjectPools;
using Mirror;
using PlayerRoles.SpawnData;
using PlayerRoles.Voice;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public class SpectatorRole : PlayerRoleBase, IPrivateSpawnDataWriter, IHealthbarRole, ISpawnDataReader, IAdvancedCameraController, ICameraController, IPoolSpawnable, IVoiceRole, IViewmodelRole, IAmbientLightRole
	{
		public override RoleTypeId RoleTypeId
		{
			get
			{
				return RoleTypeId.Spectator;
			}
		}

		public override Team Team
		{
			get
			{
				return Team.Dead;
			}
		}

		public override Color RoleColor
		{
			get
			{
				return Color.white;
			}
		}

		public float VerticalRotation
		{
			get
			{
				return SpectatorTargetTracker.CurrentOffset.rotation.x;
			}
		}

		public float HorizontalRotation
		{
			get
			{
				return SpectatorTargetTracker.CurrentOffset.rotation.y;
			}
		}

		public float RollRotation
		{
			get
			{
				return SpectatorTargetTracker.CurrentOffset.rotation.z;
			}
		}

		public Vector3 CameraPosition
		{
			get
			{
				return SpectatorTargetTracker.CurrentOffset.position;
			}
		}

		public virtual bool ReadyToRespawn
		{
			get
			{
				ReferenceHub referenceHub;
				return base.TryGetOwner(out referenceHub) && referenceHub.authManager.InstanceMode != ClientInstanceMode.DedicatedServer;
			}
		}

		public VoiceModuleBase VoiceModule { get; private set; }

		public RelativePosition DeathPosition { get; private set; }

		public uint SyncedSpectatedNetId { get; internal set; }

		public float MaxHealth
		{
			get
			{
				return 0f;
			}
		}

		public PlayerStats TargetStats
		{
			get
			{
				IHealthbarRole healthbarRole;
				if (!this.TryGetTrackedRole<IHealthbarRole>(out healthbarRole))
				{
					return null;
				}
				return healthbarRole.TargetStats;
			}
		}

		public float AmbientBoost
		{
			get
			{
				IAmbientLightRole ambientLightRole;
				if (!this.TryGetTrackedRole<IAmbientLightRole>(out ambientLightRole))
				{
					return InsufficientLighting.DefaultIntensity;
				}
				return ambientLightRole.AmbientBoost;
			}
		}

		public bool ForceBlackAmbient
		{
			get
			{
				IAmbientLightRole ambientLightRole;
				return this.TryGetTrackedRole<IAmbientLightRole>(out ambientLightRole) && ambientLightRole.ForceBlackAmbient;
			}
		}

		public bool InsufficientLight
		{
			get
			{
				return false;
			}
		}

		private bool TryGetTrackedRole<T>(out T role)
		{
			ReferenceHub referenceHub;
			if (SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub))
			{
				PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
				if (currentRole is T)
				{
					T t = currentRole as T;
					role = t;
					return true;
				}
			}
			role = default(T);
			return false;
		}

		public override void DisableRole(RoleTypeId newRole)
		{
			base.DisableRole(newRole);
			this._damageHandler = null;
			if (this._transformToRestore == null)
			{
				return;
			}
			this._transformToRestore.position = this.DeathPosition.Position;
			this._transformToRestore = null;
		}

		public void SpawnObject()
		{
			ReferenceHub referenceHub;
			if (!base.TryGetOwner(out referenceHub))
			{
				throw new InvalidOperationException("Spectator role failed to spawn - owner is null");
			}
			Transform transform = referenceHub.transform;
			this.DeathPosition = new RelativePosition(transform.position);
			transform.position = Vector3.up * 6000f;
			this.SyncedSpectatedNetId = 0U;
			if (NetworkServer.active || referenceHub.isLocalPlayer)
			{
				this._transformToRestore = transform;
			}
		}

		public void WritePrivateSpawnData(NetworkWriter writer)
		{
			if (this._damageHandler == null)
			{
				writer.WriteSpawnReason(SpectatorSpawnReason.None);
			}
			else
			{
				this._damageHandler.WriteDeathScreen(writer);
			}
			this._damageHandler = null;
		}

		public void ReadSpawnData(NetworkReader reader)
		{
			bool isLocalPlayer = base.IsLocalPlayer;
		}

		public void ServerSetData(DamageHandlerBase dhb)
		{
			this._damageHandler = dhb;
		}

		public bool TryGetViewmodelFov(out float fov)
		{
			IViewmodelRole viewmodelRole = SpectatorTargetTracker.CurrentTarget as IViewmodelRole;
			if (viewmodelRole != null && viewmodelRole != null)
			{
				return viewmodelRole.TryGetViewmodelFov(out fov);
			}
			fov = 0f;
			return false;
		}

		public SpectatorTargetTracker TrackerPrefab;

		public const float SpawnHeight = 6000f;

		private Transform _transformToRestore;

		private DamageHandlerBase _damageHandler;
	}
}
