using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HUDs;

public static class ScpHudController
{
	public static ScpHudBase CurInstance { get; private set; }

	[RuntimeInitializeOnLoadMethod]
	private static void InitOnLoad()
	{
		PlayerRoleManager.OnRoleChanged += RoleChanged;
		SpectatorTargetTracker.OnTargetChanged += TargetChanged;
	}

	private static bool ValidatePlayer(ReferenceHub hub)
	{
		if (hub.isLocalPlayer)
		{
			return true;
		}
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out var hub2))
		{
			return hub2 == hub;
		}
		if (ScpHudController.CurInstance == null)
		{
			return false;
		}
		return ScpHudController.CurInstance.Hub == hub;
	}

	private static void RoleChanged(ReferenceHub hub, PlayerRoleBase prev, PlayerRoleBase cur)
	{
		if (!ScpHudController.ValidatePlayer(hub))
		{
			return;
		}
		if (cur is IHudScp hudScp)
		{
			ScpHudController.DestroyOld();
			ScpHudController.SpawnNew(hudScp, hub);
		}
		else if (cur is SpectatorRole)
		{
			if (ScpHudController.CurInstance != null)
			{
				ScpHudController.CurInstance.OnDied();
			}
		}
		else
		{
			ScpHudController.DestroyOld();
		}
	}

	private static void TargetChanged()
	{
		if (SpectatorTargetTracker.TryGetTrackedPlayer(out var hub))
		{
			ScpHudController.DestroyOld();
			if (hub.roleManager.CurrentRole is IHudScp hudScp)
			{
				ScpHudController.SpawnNew(hudScp, hub);
			}
		}
	}

	private static void DestroyOld()
	{
		if (!(ScpHudController.CurInstance == null))
		{
			Object.Destroy(ScpHudController.CurInstance.gameObject);
		}
	}

	private static void SpawnNew(IHudScp hudScp, ReferenceHub owner)
	{
		ScpHudController.CurInstance = Object.Instantiate(hudScp.HudPrefab);
		ScpHudController.CurInstance.Init(owner);
	}
}
