using System;
using System.Collections.Generic;
using Mirror;
using NetworkManagerUtils;
using NorthwoodLib.Pools;
using UnityEngine;

namespace GameCore
{
	public static class DummyUtils
	{
		public static UserGroup DummyGroup { get; set; } = new UserGroup
		{
			BadgeColor = "pumpkin",
			BadgeText = "Dummy",
			Permissions = 0UL,
			KickPower = 0,
			Cover = true,
			HiddenByDefault = false,
			Shared = false,
			RequiredKickPower = byte.MaxValue
		};

		public static ReferenceHub SpawnDummy(string nickname = "Dummy")
		{
			GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(NetworkManager.singleton.playerPrefab);
			ReferenceHub referenceHub;
			if (!gameObject.TryGetComponent<ReferenceHub>(out referenceHub))
			{
				return null;
			}
			referenceHub.nicknameSync.MyNick = nickname;
			NetworkServer.AddPlayerForConnection(new DummyNetworkConnection(), gameObject);
			return referenceHub;
		}

		public static void DestroyAllDummies()
		{
			List<ReferenceHub> list = ListPool<ReferenceHub>.Shared.Rent();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.IsDummy)
				{
					list.Add(referenceHub);
				}
			}
			foreach (ReferenceHub referenceHub2 in list)
			{
				NetworkServer.Destroy(referenceHub2.gameObject);
			}
			ListPool<ReferenceHub>.Shared.Return(list);
		}
	}
}
