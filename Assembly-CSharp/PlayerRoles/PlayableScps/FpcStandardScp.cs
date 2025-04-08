using System;
using PlayerRoles.Blood;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using UnityEngine;

namespace PlayerRoles.PlayableScps
{
	public class FpcStandardScp : FpcStandardRoleBase, IBleedableRole
	{
		public BloodSettings BloodSettings { get; private set; }

		public override RoleTypeId RoleTypeId
		{
			get
			{
				return this._roleTypeId;
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

		public override float MaxHealth
		{
			get
			{
				return (float)this._maxHealth;
			}
		}

		public override ISpawnpointHandler SpawnpointHandler
		{
			get
			{
				if (this._disableSpawnpoint)
				{
					return null;
				}
				if (this._cachedSpawnpoint == null)
				{
					this._cachedSpawnpoint = new RoomRoleSpawnpoint(this._roomSpawnpoint);
				}
				return this._cachedSpawnpoint;
			}
		}

		public override float AmbientBoost
		{
			get
			{
				if (!this.InsufficientLight)
				{
					return base.AmbientBoost;
				}
				return 0.2f;
			}
		}

		private RoomRoleSpawnpoint _cachedSpawnpoint;

		[SerializeField]
		private RoleTypeId _roleTypeId;

		[SerializeField]
		private int _maxHealth;

		[SerializeField]
		private RoomRoleSpawnpoint _roomSpawnpoint;

		[SerializeField]
		private bool _disableSpawnpoint;
	}
}
