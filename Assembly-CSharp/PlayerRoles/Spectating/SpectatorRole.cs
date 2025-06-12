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
using UserSettings;
using UserSettings.ControlsSettings;

namespace PlayerRoles.Spectating;

public class SpectatorRole : PlayerRoleBase, IPrivateSpawnDataWriter, IHealthbarRole, IVoiceRole, ISpawnDataReader, IAdvancedCameraController, ICameraController, IPoolSpawnable, IPoolResettable, IViewmodelRole, IAmbientLightRole
{
	public const float SpawnHeight = 6000f;

	private const float AutomaticSpectatorSwitchDelay = 5f;

	public static readonly CachedUserSetting<bool> AutomaticSpectatorSwitch = new CachedUserSetting<bool>(MiscControlsSetting.AutomaticSpectatorSwitch);

	public SpectatorTargetTracker TrackerPrefab;

	private DamageHandlerBase _damageHandler;

	private Transform _transformToRestore;

	private float _timeTillAutomaticSwitch;

	private bool _switchAutomatically;

	private uint? _killerId;

	public override RoleTypeId RoleTypeId => RoleTypeId.Spectator;

	public override Team Team => Team.Dead;

	public override Color RoleColor => Color.white;

	public float VerticalRotation => SpectatorTargetTracker.CurrentOffset.rotation.x;

	public float HorizontalRotation => SpectatorTargetTracker.CurrentOffset.rotation.y;

	public float RollRotation => SpectatorTargetTracker.CurrentOffset.rotation.z;

	public Vector3 CameraPosition => SpectatorTargetTracker.CurrentOffset.position;

	public virtual bool ReadyToRespawn
	{
		get
		{
			if (base.TryGetOwner(out var hub))
			{
				return hub.authManager.InstanceMode != ClientInstanceMode.DedicatedServer;
			}
			return false;
		}
	}

	[field: SerializeField]
	public VoiceModuleBase VoiceModule { get; private set; }

	public RelativePosition DeathPosition { get; private set; }

	public uint SyncedSpectatedNetId { get; internal set; }

	public float MaxHealth => 0f;

	public PlayerStats TargetStats
	{
		get
		{
			if (!SpectatorRole.TryGetTrackedRole<IHealthbarRole>(out var role))
			{
				return null;
			}
			return role.TargetStats;
		}
	}

	public float AmbientBoost
	{
		get
		{
			if (!SpectatorRole.TryGetTrackedRole<IAmbientLightRole>(out var role))
			{
				return InsufficientLighting.DefaultIntensity;
			}
			return role.AmbientBoost;
		}
	}

	public bool ForceBlackAmbient
	{
		get
		{
			if (SpectatorRole.TryGetTrackedRole<IAmbientLightRole>(out var role))
			{
				return role.ForceBlackAmbient;
			}
			return false;
		}
	}

	public bool InsufficientLight => false;

	public override void DisableRole(RoleTypeId newRole)
	{
		base.DisableRole(newRole);
		this._damageHandler = null;
		if (!(this._transformToRestore == null))
		{
			this._transformToRestore.position = this.DeathPosition.Position;
			this._transformToRestore = null;
		}
	}

	public void SpawnObject()
	{
		if (!base.TryGetOwner(out var hub))
		{
			throw new InvalidOperationException("Spectator role failed to spawn - owner is null");
		}
		Transform transform = hub.transform;
		this.DeathPosition = new RelativePosition(transform.position);
		transform.position = Vector3.up * 6000f;
		this.SyncedSpectatedNetId = 0u;
		if (NetworkServer.active || hub.isLocalPlayer)
		{
			this._transformToRestore = transform;
		}
		SpectatorTargetTracker.OnTargetChanged += ResetAutomaticSwitch;
	}

	public void ResetObject()
	{
		SpectatorTargetTracker.OnTargetChanged -= ResetAutomaticSwitch;
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
		_ = base.IsLocalPlayer;
	}

	public void ServerSetData(DamageHandlerBase dhb)
	{
		this._damageHandler = dhb;
	}

	public bool TryGetViewmodelFov(out float fov)
	{
		if (SpectatorTargetTracker.CurrentTarget is IViewmodelRole viewmodelRole && viewmodelRole != null)
		{
			return viewmodelRole.TryGetViewmodelFov(out fov);
		}
		fov = 0f;
		return false;
	}

	protected virtual void ScheduleNextTarget(uint killerId)
	{
	}

	protected virtual void Update()
	{
	}

	private void ResetAutomaticSwitch()
	{
		this._timeTillAutomaticSwitch = 0f;
		this._switchAutomatically = false;
		this._killerId = null;
	}

	private bool TryGetKiller(out ISpectatableRole spectatableRole)
	{
		spectatableRole = null;
		if (!this._killerId.HasValue)
		{
			return false;
		}
		if (!ReferenceHub.TryGetHubNetID(this._killerId.Value, out var hub))
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is ISpectatableRole spectatableRole2))
		{
			return false;
		}
		spectatableRole = spectatableRole2;
		return true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		UserSetting<bool>.SetDefaultValue(MiscControlsSetting.AutomaticSpectatorSwitch, defaultValue: true);
	}

	private static bool TryGetTrackedRole<T>(out T role)
	{
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out var hub) && hub.roleManager.CurrentRole is T val)
		{
			role = val;
			return true;
		}
		role = default(T);
		return false;
	}
}
