using Interactables.Interobjects.DoorUtils;
using PlayerRoles.Blood;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.RoleAssign;
using UnityEngine;

namespace PlayerRoles.PlayableScps;

public class FpcStandardScp : FpcStandardRoleBase, IBleedableRole, IDoorPermissionProvider
{
	private RoomRoleSpawnpoint _cachedSpawnpoint;

	[SerializeField]
	private RoleTypeId _roleTypeId;

	[SerializeField]
	private int _maxHealth;

	[SerializeField]
	private RoomRoleSpawnpoint _roomSpawnpoint;

	[SerializeField]
	private bool _disableSpawnpoint;

	[field: SerializeField]
	public BloodSettings BloodSettings { get; private set; }

	public override RoleTypeId RoleTypeId => _roleTypeId;

	public override Team Team => Team.SCPs;

	public override Color RoleColor => Color.red;

	public override float MaxHealth
	{
		get
		{
			if (!RoleAssigner.ScpsOverflowing)
			{
				return _maxHealth;
			}
			return (float)_maxHealth * 1.1f;
		}
	}

	public override ISpawnpointHandler SpawnpointHandler
	{
		get
		{
			if (_disableSpawnpoint)
			{
				return null;
			}
			if (_cachedSpawnpoint == null)
			{
				_cachedSpawnpoint = new RoomRoleSpawnpoint(_roomSpawnpoint);
			}
			return _cachedSpawnpoint;
		}
	}

	public override float AmbientBoost
	{
		get
		{
			if (!InsufficientLight)
			{
				return base.AmbientBoost;
			}
			return 0.2f;
		}
	}

	public PermissionUsed PermissionsUsedCallback => null;

	public DoorPermissionFlags GetPermissions(IDoorPermissionRequester requester)
	{
		return DoorPermissionFlags.ScpOverride;
	}
}
