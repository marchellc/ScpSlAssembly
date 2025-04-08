using System;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HUDs
{
	public static class ScpHudController
	{
		public static ScpHudBase CurInstance { get; private set; }

		[RuntimeInitializeOnLoadMethod]
		private static void InitOnLoad()
		{
			PlayerRoleManager.OnRoleChanged += ScpHudController.RoleChanged;
			SpectatorTargetTracker.OnTargetChanged += ScpHudController.TargetChanged;
		}

		private static bool ValidatePlayer(ReferenceHub hub)
		{
			if (hub.isLocalPlayer)
			{
				return true;
			}
			ReferenceHub referenceHub;
			if (SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub))
			{
				return referenceHub == hub;
			}
			return !(ScpHudController.CurInstance == null) && ScpHudController.CurInstance.Hub == hub;
		}

		private static void RoleChanged(ReferenceHub hub, PlayerRoleBase prev, PlayerRoleBase cur)
		{
			if (!ScpHudController.ValidatePlayer(hub))
			{
				return;
			}
			IHudScp hudScp = cur as IHudScp;
			if (hudScp != null)
			{
				ScpHudController.DestroyOld();
				ScpHudController.SpawnNew(hudScp, hub);
				return;
			}
			if (cur is SpectatorRole)
			{
				if (ScpHudController.CurInstance != null)
				{
					ScpHudController.CurInstance.OnDied();
					return;
				}
			}
			else
			{
				ScpHudController.DestroyOld();
			}
		}

		private static void TargetChanged()
		{
			ReferenceHub referenceHub;
			if (!SpectatorTargetTracker.TryGetTrackedPlayer(out referenceHub))
			{
				return;
			}
			ScpHudController.DestroyOld();
			IHudScp hudScp = referenceHub.roleManager.CurrentRole as IHudScp;
			if (hudScp != null)
			{
				ScpHudController.SpawnNew(hudScp, referenceHub);
			}
		}

		private static void DestroyOld()
		{
			if (ScpHudController.CurInstance == null)
			{
				return;
			}
			global::UnityEngine.Object.Destroy(ScpHudController.CurInstance.gameObject);
		}

		private static void SpawnNew(IHudScp hudScp, ReferenceHub owner)
		{
			ScpHudController.CurInstance = global::UnityEngine.Object.Instantiate<ScpHudBase>(hudScp.HudPrefab);
			ScpHudController.CurInstance.Init(owner);
		}
	}
}
