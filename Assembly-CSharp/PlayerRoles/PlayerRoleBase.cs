using System;
using System.Diagnostics;
using GameObjectPools;
using Mirror;
using UnityEngine;

namespace PlayerRoles
{
	public abstract class PlayerRoleBase : PoolObject
	{
		public abstract RoleTypeId RoleTypeId { get; }

		public abstract Team Team { get; }

		public abstract Color RoleColor { get; }

		public virtual GameObject RoleHelpInfo { get; private set; }

		public string RoleName
		{
			get
			{
				ICustomNameRole customNameRole = this as ICustomNameRole;
				if (customNameRole == null)
				{
					return RoleTranslations.GetRoleName(this.RoleTypeId);
				}
				return customNameRole.CustomRoleName;
			}
		}

		public float ActiveTime
		{
			get
			{
				return (float)this._activeTime.Elapsed.TotalSeconds;
			}
		}

		public bool IsLocalPlayer
		{
			get
			{
				ReferenceHub referenceHub;
				return this.TryGetOwner(out referenceHub) && referenceHub.isLocalPlayer;
			}
		}

		public int UniqueLifeIdentifier { get; private set; }

		public RoleChangeReason ServerSpawnReason
		{
			get
			{
				if (!NetworkServer.active)
				{
					global::UnityEngine.Debug.LogError("Server-only property ServerSpawnReason cannot be called on the client!");
				}
				return this._spawnReason;
			}
			private set
			{
				this._spawnReason = value;
			}
		}

		public RoleSpawnFlags ServerSpawnFlags
		{
			get
			{
				if (!NetworkServer.active)
				{
					global::UnityEngine.Debug.LogError("Server-only property ServerSpawnFlags cannot be called on the client!");
				}
				return this._spawnFlags;
			}
			set
			{
				this._spawnFlags = value;
			}
		}

		internal virtual void Init(ReferenceHub hub, RoleChangeReason spawnReason, RoleSpawnFlags spawnFlags)
		{
			this._lastOwner = hub;
			this._spawnFlags = spawnFlags;
			this._spawnReason = spawnReason;
			this._activeTime.Restart();
			this.UniqueLifeIdentifier = ++PlayerRoleBase._lastAssignedLifeIdentifier;
		}

		public bool TryGetOwner(out ReferenceHub hub)
		{
			hub = this._lastOwner;
			return !base.Pooled;
		}

		public virtual void DisableRole(RoleTypeId newRole)
		{
			try
			{
				Action<RoleTypeId> onRoleDisabled = this.OnRoleDisabled;
				if (onRoleDisabled != null)
				{
					onRoleDisabled(newRole);
				}
				base.ReturnToPool(true);
			}
			catch (Exception ex)
			{
				global::UnityEngine.Debug.Log(string.Format("Disabling {0} role has thrown an exception while switching to {1}.", this.RoleTypeId, newRole));
				global::UnityEngine.Debug.LogException(ex);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} (RoleTypeId = '{1}', Owner = '{2}', ActiveTime = '{3}')", new object[] { "PlayerRoleBase", this.RoleTypeId, this._lastOwner, this.ActiveTime });
		}

		private ReferenceHub _lastOwner;

		private RoleSpawnFlags _spawnFlags;

		private RoleChangeReason _spawnReason;

		private readonly Stopwatch _activeTime = Stopwatch.StartNew();

		private static int _lastAssignedLifeIdentifier;

		public Action<RoleTypeId> OnRoleDisabled;
	}
}
