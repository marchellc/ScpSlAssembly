using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using Respawning;
using Respawning.Waves;
using UnityEngine;

namespace Christmas.Scp2536.Gifts;

public class BuddyInABox : Scp2536ItemGift
{
	private const int MinimumSpectators = 2;

	private const float MaxSpawnDistance = 3f;

	private static readonly Dictionary<Team, RoleTypeId> TeamToRole = new Dictionary<Team, RoleTypeId>
	{
		[Team.ClassD] = RoleTypeId.ChaosRifleman,
		[Team.Scientists] = RoleTypeId.NtfPrivate
	};

	private static bool _waveSpawnedAlready;

	public override UrgencyLevel Urgency => UrgencyLevel.Three;

	protected override Scp2536Reward[] Rewards => new Scp2536Reward[1]
	{
		new Scp2536Reward(ItemType.Adrenaline, 100f)
	};

	public override bool CanBeGranted(ReferenceHub hub)
	{
		if (!base.CanBeGranted(hub))
		{
			return false;
		}
		RoleTypeId roleId = hub.GetRoleId();
		if (!TeamToRole.ContainsKey(hub.GetTeam()))
		{
			return false;
		}
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (roleId == allHub.GetRoleId())
			{
				return false;
			}
			if (allHub.roleManager.CurrentRole is SpectatorRole { ReadyToRespawn: not false })
			{
				num++;
			}
		}
		return num < 2;
	}

	public override void ServerGrant(ReferenceHub hub)
	{
		if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return;
		}
		Vector3 forward = hub.PlayerCameraReference.forward;
		Vector3 position = fpcRole.FpcModule.Position;
		Team team = hub.GetTeam();
		RoleTypeId newRole = TeamToRole[team];
		int num = 0;
		foreach (ReferenceHub availablePlayer in WaveSpawner.GetAvailablePlayers(team))
		{
			if (num >= 2)
			{
				break;
			}
			availablePlayer.roleManager.ServerSetRole(newRole, RoleChangeReason.ItemUsage, ~RoleSpawnFlags.UseSpawnpoint);
			if (availablePlayer.roleManager.CurrentRole is IFpcRole fpcRole2)
			{
				Vector3 safePosition = SafeLocationFinder.GetSafePosition(position, forward, 3f, fpcRole2.FpcModule.CharController);
				Vector3 eulerAngles = Quaternion.LookRotation(position - safePosition).eulerAngles;
				fpcRole2.FpcModule.ServerOverridePosition(safePosition);
				fpcRole2.FpcModule.ServerOverrideRotation(eulerAngles);
				num++;
			}
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		WaveManager.OnWaveSpawned += delegate
		{
			_waveSpawnedAlready = true;
		};
		CustomNetworkManager.OnClientReady += delegate
		{
			_waveSpawnedAlready = false;
		};
	}
}
