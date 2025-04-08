using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;
using UserSettings;
using UserSettings.VideoSettings;
using Utils.NonAllocLINQ;

namespace PlayerRoles.Ragdolls
{
	public static class RagdollManager
	{
		public static event Action<ReferenceHub, BasicRagdoll> ServerOnRagdollCreated;

		public static event Action<BasicRagdoll> OnRagdollSpawned;

		public static event Action<BasicRagdoll> OnRagdollRemoved;

		public static int FreezeTime { get; set; }

		internal static void OnSpawnedRagdoll(BasicRagdoll ragdoll)
		{
			RagdollManager.AllRagdolls.Add(ragdoll);
			Action<BasicRagdoll> onRagdollSpawned = RagdollManager.OnRagdollSpawned;
			if (onRagdollSpawned == null)
			{
				return;
			}
			onRagdollSpawned(ragdoll);
		}

		internal static void OnRemovedRagdoll(BasicRagdoll ragdoll)
		{
			RagdollManager.AllRagdolls.Remove(ragdoll);
			Action<BasicRagdoll> onRagdollRemoved = RagdollManager.OnRagdollRemoved;
			if (onRagdollRemoved == null)
			{
				return;
			}
			onRagdollRemoved(ragdoll);
		}

		private static HashSet<NetworkIdentity> AllRagdollPrefabs
		{
			get
			{
				if (RagdollManager._prefabsCacheSet)
				{
					return RagdollManager.CachedRagdollPrefabs;
				}
				foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
				{
					IRagdollRole ragdollRole = keyValuePair.Value as IRagdollRole;
					if (ragdollRole != null)
					{
						RagdollManager.CachedRagdollPrefabs.Add(ragdollRole.Ragdoll.netIdentity);
					}
				}
				RagdollManager._prefabsCacheSet = true;
				return RagdollManager.CachedRagdollPrefabs;
			}
		}

		public static BasicRagdoll ServerSpawnRagdoll(ReferenceHub owner, DamageHandlerBase handler)
		{
			if (!NetworkServer.active || owner == null)
			{
				return null;
			}
			IRagdollRole ragdollRole = owner.roleManager.CurrentRole as IRagdollRole;
			if (ragdollRole == null)
			{
				return null;
			}
			PlayerSpawningRagdollEventArgs playerSpawningRagdollEventArgs = new PlayerSpawningRagdollEventArgs(owner, ragdollRole.Ragdoll, handler);
			PlayerEvents.OnSpawningRagdoll(playerSpawningRagdollEventArgs);
			if (!playerSpawningRagdollEventArgs.IsAllowed)
			{
				return null;
			}
			BasicRagdoll basicRagdoll = ragdollRole.Ragdoll.ServerInstantiateSelf(owner, owner.GetRoleId());
			Transform transform = ragdollRole.Ragdoll.transform;
			basicRagdoll.NetworkInfo = new RagdollData(owner, handler, transform.localPosition, transform.localRotation, 0);
			Action<ReferenceHub, BasicRagdoll> serverOnRagdollCreated = RagdollManager.ServerOnRagdollCreated;
			if (serverOnRagdollCreated != null)
			{
				serverOnRagdollCreated(owner, basicRagdoll);
			}
			NetworkServer.Spawn(basicRagdoll.gameObject, null);
			PlayerEvents.OnSpawnedRagdoll(new PlayerSpawnedRagdollEventArgs(owner, ragdollRole.Ragdoll, handler));
			return basicRagdoll;
		}

		public static BasicRagdoll ServerCreateRagdoll(RoleTypeId role, Vector3 position, Quaternion rotation, DamageHandlerBase handler, string nickname, Vector3? scale = null, ushort serial = 0)
		{
			if (scale == null)
			{
				scale = new Vector3?(Vector3.one);
			}
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
			{
				return null;
			}
			IRagdollRole ragdollRole = playerRoleBase as IRagdollRole;
			if (ragdollRole == null)
			{
				return null;
			}
			BasicRagdoll basicRagdoll = ragdollRole.Ragdoll.ServerInstantiateSelf(null, role);
			basicRagdoll.NetworkInfo = new RagdollData(null, handler, role, position, rotation, scale.Value, nickname, NetworkTime.time, serial);
			Action<ReferenceHub, BasicRagdoll> serverOnRagdollCreated = RagdollManager.ServerOnRagdollCreated;
			if (serverOnRagdollCreated != null)
			{
				serverOnRagdollCreated(null, basicRagdoll);
			}
			NetworkServer.Spawn(basicRagdoll.gameObject, null);
			return basicRagdoll;
		}

		public static Vector3 GetDefaultScale(RoleTypeId role)
		{
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
			{
				return Vector3.zero;
			}
			IRagdollRole ragdollRole = playerRoleBase as IRagdollRole;
			if (ragdollRole == null)
			{
				return Vector3.zero;
			}
			return ragdollRole.Ragdoll.transform.localScale;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientStarted += delegate
			{
				RagdollManager.AllRagdollPrefabs.ForEach(new Action<NetworkIdentity>(RagdollManager.RegisterPrefab));
			};
			UserSetting<bool>.AddListener<PerformanceVideoSetting>(PerformanceVideoSetting.RagdollFreeze, delegate(bool _)
			{
				RagdollManager.UpdateCleanupPrefs();
			});
			UserSetting<float>.AddListener<PerformanceVideoSetting>(PerformanceVideoSetting.RagdollFreeze, delegate(float _)
			{
				RagdollManager.UpdateCleanupPrefs();
			});
			RagdollManager.UpdateCleanupPrefs();
		}

		private static void RegisterPrefab(NetworkIdentity nid)
		{
			if (NetworkClient.prefabs.ContainsKey(nid.assetId))
			{
				return;
			}
			NetworkClient.UnregisterSpawnHandler(nid.assetId);
			BasicRagdoll component = nid.GetComponent<BasicRagdoll>();
			NetworkClient.RegisterPrefab(nid.gameObject, new SpawnHandlerDelegate(component.ClientHandleSpawn), new UnSpawnDelegate(component.ClientHandleDespawn));
		}

		private static void UpdateCleanupPrefs()
		{
			bool flag = UserSetting<bool>.Get<PerformanceVideoSetting>(PerformanceVideoSetting.RagdollFreeze);
			int num = Mathf.RoundToInt(UserSetting<float>.Get<PerformanceVideoSetting>(PerformanceVideoSetting.RagdollFreeze));
			RagdollManager.FreezeTime = (flag ? num : int.MaxValue);
		}

		public static readonly HashSet<BasicRagdoll> AllRagdolls = new HashSet<BasicRagdoll>();

		private static readonly HashSet<NetworkIdentity> CachedRagdollPrefabs = new HashSet<NetworkIdentity>();

		private static bool _prefabsCacheSet;
	}
}
