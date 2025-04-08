using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using GameObjectPools;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.SpawnData;
using UnityEngine;

namespace PlayerRoles
{
	public sealed class PlayerRoleManager : NetworkBehaviour
	{
		public static event PlayerRoleManager.ServerRoleSet OnServerRoleSet;

		public static event PlayerRoleManager.RoleChanged OnRoleChanged;

		public PlayerRoleBase CurrentRole
		{
			get
			{
				if (!this._anySet)
				{
					this.InitializeNewRole(RoleTypeId.None, RoleChangeReason.None, RoleSpawnFlags.All, null);
				}
				return this._curRole;
			}
			set
			{
				this._curRole = value;
				this._anySet = true;
			}
		}

		private ReferenceHub Hub
		{
			get
			{
				if (!this._hubSet && ReferenceHub.TryGetHub(base.gameObject, out this._hub))
				{
					this._hubSet = true;
				}
				return this._hub;
			}
		}

		private void Update()
		{
			if (!NetworkServer.active || !this._sendNextFrame)
			{
				return;
			}
			this._sendNextFrame = false;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (!referenceHub.isLocalPlayer)
				{
					RoleTypeId roleTypeId = this.CurrentRole.RoleTypeId;
					IObfuscatedRole obfuscatedRole = this.CurrentRole as IObfuscatedRole;
					if (obfuscatedRole != null)
					{
						roleTypeId = obfuscatedRole.GetRoleForUser(referenceHub);
						RoleTypeId roleTypeId2;
						if (this.PreviouslySentRole.TryGetValue(referenceHub.netId, out roleTypeId2) && roleTypeId2 == roleTypeId)
						{
							continue;
						}
					}
					referenceHub.connectionToClient.Send<RoleSyncInfo>(new RoleSyncInfo(this.Hub, roleTypeId, referenceHub), 0);
					this.PreviouslySentRole[referenceHub.netId] = roleTypeId;
				}
			}
		}

		private void OnDestroy()
		{
			DestroyedRole destroyedRole = this.CurrentRole as DestroyedRole;
			if (destroyedRole != null && destroyedRole != null)
			{
				destroyedRole.DisableRole(RoleTypeId.Destroyed);
			}
		}

		public override void OnStopClient()
		{
			base.OnStopClient();
			this.InitializeNewRole(RoleTypeId.Destroyed, RoleChangeReason.Destroyed, RoleSpawnFlags.All, null);
		}

		private PlayerRoleBase GetRoleBase(RoleTypeId targetId)
		{
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(targetId, out playerRoleBase))
			{
				Debug.LogError(string.Format("Role #{0} could not be found. Player with ID {1} will receive the default role ({2}).", targetId, this.Hub.PlayerId, RoleTypeId.None));
				if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(RoleTypeId.None, out playerRoleBase))
				{
					throw new NotImplementedException("Role change failed. Default role is not correctly implemented.");
				}
			}
			PoolObject poolObject;
			if (PoolManager.Singleton.TryGetPoolObject(playerRoleBase.gameObject, out poolObject, false))
			{
				PlayerRoleBase playerRoleBase2 = poolObject as PlayerRoleBase;
				if (playerRoleBase2 != null)
				{
					return playerRoleBase2;
				}
			}
			throw new InvalidOperationException(string.Format("Role {0} failed to initialize, pool was not found or dequed object was of incorrect type.", targetId));
		}

		public void InitializeNewRole(RoleTypeId targetId, RoleChangeReason reason, RoleSpawnFlags spawnFlags = RoleSpawnFlags.All, NetworkReader data = null)
		{
			PlayerRoleBase playerRoleBase;
			bool flag;
			if (this._anySet)
			{
				playerRoleBase = this.CurrentRole;
				playerRoleBase.DisableRole(targetId);
				flag = true;
			}
			else
			{
				playerRoleBase = null;
				flag = false;
			}
			PlayerRoleBase roleBase = this.GetRoleBase(targetId);
			Transform transform = roleBase.transform;
			if (targetId != RoleTypeId.Destroyed || reason != RoleChangeReason.Destroyed)
			{
				transform.parent = base.transform;
				transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			}
			this.CurrentRole = roleBase;
			roleBase.Init(this.Hub, reason, spawnFlags);
			RoleSpawnpointManager.SetPosition(this.Hub, this.CurrentRole);
			roleBase.SetupPoolObject();
			ISpawnDataReader spawnDataReader = this.CurrentRole as ISpawnDataReader;
			if (spawnDataReader != null && data != null && targetId != RoleTypeId.Spectator && !base.isLocalPlayer)
			{
				spawnDataReader.ReadSpawnData(data);
			}
			if (flag)
			{
				PlayerRoleManager.RoleChanged onRoleChanged = PlayerRoleManager.OnRoleChanged;
				if (onRoleChanged != null)
				{
					onRoleChanged(this.Hub, playerRoleBase, this.CurrentRole);
				}
			}
			SpawnProtected.TryGiveProtection(this.Hub);
		}

		[Server]
		public void ServerSetRole(RoleTypeId newRole, RoleChangeReason reason, RoleSpawnFlags spawnFlags = RoleSpawnFlags.All)
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayerRoleManager::ServerSetRole(PlayerRoles.RoleTypeId,PlayerRoles.RoleChangeReason,PlayerRoles.RoleSpawnFlags)' called when server was not active");
				return;
			}
			PlayerChangingRoleEventArgs playerChangingRoleEventArgs = new PlayerChangingRoleEventArgs(this.Hub, this.CurrentRole, newRole, reason);
			PlayerEvents.OnChangingRole(playerChangingRoleEventArgs);
			if (!playerChangingRoleEventArgs.IsAllowed)
			{
				return;
			}
			newRole = playerChangingRoleEventArgs.NewRole;
			reason = playerChangingRoleEventArgs.ChangeReason;
			PlayerRoleManager.ServerRoleSet onServerRoleSet = PlayerRoleManager.OnServerRoleSet;
			if (onServerRoleSet != null)
			{
				onServerRoleSet(this.Hub, newRole, reason);
			}
			this.InitializeNewRole(newRole, reason, spawnFlags, null);
			this._sendNextFrame = true;
			PlayerEvents.OnChangedRole(new PlayerChangedRoleEventArgs(this._hub, this.CurrentRole, newRole, reason));
		}

		public override bool Weaved()
		{
			return true;
		}

		public readonly Dictionary<uint, RoleTypeId> PreviouslySentRole = new Dictionary<uint, RoleTypeId>();

		private ReferenceHub _hub;

		private bool _hubSet;

		private bool _anySet;

		private bool _sendNextFrame;

		private PlayerRoleBase _curRole;

		private const RoleTypeId DefaultRole = RoleTypeId.None;

		public delegate void ServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason);

		public delegate void RoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole);
	}
}
