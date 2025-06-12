using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using GameObjectPools;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles.FirstPersonControl.Spawnpoints;
using PlayerRoles.SpawnData;
using UnityEngine;

namespace PlayerRoles;

public sealed class PlayerRoleManager : NetworkBehaviour, IRootDummyActionProvider
{
	public delegate void ServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason);

	public delegate void RoleChanged(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole);

	public readonly Dictionary<uint, RoleTypeId> PreviouslySentRole = new Dictionary<uint, RoleTypeId>();

	private ReferenceHub _hub;

	private bool _hubSet;

	private bool _anySet;

	private bool _sendNextFrame;

	private PlayerRoleBase _curRole;

	private IRootDummyActionProvider[] _dummyProviders;

	private const RoleTypeId DefaultRole = RoleTypeId.None;

	public PlayerRoleBase CurrentRole
	{
		get
		{
			if (!this._anySet)
			{
				this.InitializeNewRole(RoleTypeId.None, RoleChangeReason.None);
			}
			return this._curRole;
		}
		set
		{
			this._curRole = value;
			this._anySet = true;
		}
	}

	public bool DummyActionsDirty { get; set; }

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

	public static event ServerRoleSet OnServerRoleSet;

	public static event RoleChanged OnRoleChanged;

	private void Update()
	{
		if (!NetworkServer.active || !this._sendNextFrame)
		{
			return;
		}
		this._sendNextFrame = false;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.isLocalPlayer)
			{
				continue;
			}
			RoleTypeId roleTypeId = this.CurrentRole.RoleTypeId;
			if (this.CurrentRole is IObfuscatedRole obfuscatedRole)
			{
				roleTypeId = obfuscatedRole.GetRoleForUser(allHub);
				if (this.PreviouslySentRole.TryGetValue(allHub.netId, out var value) && value == roleTypeId)
				{
					continue;
				}
			}
			allHub.connectionToClient.Send(new RoleSyncInfo(this.Hub, roleTypeId, allHub));
			this.PreviouslySentRole[allHub.netId] = roleTypeId;
		}
	}

	private void OnDestroy()
	{
		if (this.CurrentRole is DestroyedRole destroyedRole && destroyedRole != null)
		{
			destroyedRole.DisableRole(RoleTypeId.Destroyed);
		}
	}

	public override void OnStopClient()
	{
		base.OnStopClient();
		this.InitializeNewRole(RoleTypeId.Destroyed, RoleChangeReason.Destroyed);
	}

	private PlayerRoleBase GetRoleBase(RoleTypeId targetId)
	{
		if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(targetId, out var result))
		{
			Debug.LogError($"Role #{targetId} could not be found. Player with ID {this.Hub.PlayerId} will receive the default role ({RoleTypeId.None}).");
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(RoleTypeId.None, out result))
			{
				throw new NotImplementedException("Role change failed. Default role is not correctly implemented.");
			}
		}
		if (!PoolManager.Singleton.TryGetPoolObject(result.gameObject, out var poolObject, autoSetup: false) || !(poolObject is PlayerRoleBase result2))
		{
			throw new InvalidOperationException($"Role {targetId} failed to initialize, pool was not found or dequed object was of incorrect type.");
		}
		return result2;
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
			transform.ResetLocalPose();
		}
		this.CurrentRole = roleBase;
		roleBase.Init(this.Hub, reason, spawnFlags);
		RoleSpawnpointManager.SetPosition(this.Hub, this.CurrentRole);
		roleBase.SetupPoolObject();
		if (this.CurrentRole is ISpawnDataReader spawnDataReader && data != null && targetId != RoleTypeId.Spectator && !base.isLocalPlayer)
		{
			spawnDataReader.ReadSpawnData(data);
		}
		if (flag)
		{
			PlayerRoleManager.OnRoleChanged?.Invoke(this.Hub, playerRoleBase, this.CurrentRole);
		}
		SpawnProtected.TryGiveProtection(this.Hub);
		if (this.Hub.IsDummy)
		{
			this._dummyProviders = roleBase.GetComponentsInChildren<IRootDummyActionProvider>();
			this.DummyActionsDirty = true;
		}
	}

	[Server]
	public void ServerSetRole(RoleTypeId newRole, RoleChangeReason reason, RoleSpawnFlags spawnFlags = RoleSpawnFlags.All)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void PlayerRoles.PlayerRoleManager::ServerSetRole(PlayerRoles.RoleTypeId,PlayerRoles.RoleChangeReason,PlayerRoles.RoleSpawnFlags)' called when server was not active");
			return;
		}
		PlayerChangingRoleEventArgs e = new PlayerChangingRoleEventArgs(this.Hub, this.CurrentRole, newRole, reason, spawnFlags);
		PlayerEvents.OnChangingRole(e);
		if (e.IsAllowed)
		{
			newRole = e.NewRole;
			reason = e.ChangeReason;
			spawnFlags = e.SpawnFlags;
			RoleTypeId roleTypeId = this.CurrentRole.RoleTypeId;
			PlayerRoleManager.OnServerRoleSet?.Invoke(this.Hub, newRole, reason);
			this.InitializeNewRole(newRole, reason, spawnFlags);
			this._sendNextFrame = true;
			PlayerEvents.OnChangedRole(new PlayerChangedRoleEventArgs(this._hub, roleTypeId, this.CurrentRole, reason, spawnFlags));
		}
	}

	public void PopulateDummyActions(Action<DummyAction> actionAdder, Action<string> categoryAdder)
	{
		IRootDummyActionProvider[] dummyProviders = this._dummyProviders;
		for (int i = 0; i < dummyProviders.Length; i++)
		{
			dummyProviders[i].PopulateDummyActions(actionAdder, categoryAdder);
		}
		this.DummyActionsDirty = false;
	}

	public override bool Weaved()
	{
		return true;
	}
}
