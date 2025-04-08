using System;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using Respawning;
using Respawning.Waves;
using UnityEngine;

namespace Christmas.Scp2536.Gifts
{
	public class BuddyInABox : Scp2536ItemGift
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Three;
			}
		}

		protected override Scp2536Reward[] Rewards
		{
			get
			{
				return new Scp2536Reward[]
				{
					new Scp2536Reward(ItemType.Adrenaline, 100f)
				};
			}
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			if (!base.CanBeGranted(hub))
			{
				return false;
			}
			RoleTypeId roleId = hub.GetRoleId();
			if (!BuddyInABox.TeamToRole.ContainsKey(hub.GetTeam()))
			{
				return false;
			}
			int num = 0;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (roleId == referenceHub.GetRoleId())
				{
					return false;
				}
				SpectatorRole spectatorRole = referenceHub.roleManager.CurrentRole as SpectatorRole;
				if (spectatorRole != null && spectatorRole.ReadyToRespawn)
				{
					num++;
				}
			}
			return num < 2;
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			Vector3 forward = hub.PlayerCameraReference.forward;
			Vector3 position = fpcRole.FpcModule.Position;
			Team team = hub.GetTeam();
			RoleTypeId roleTypeId = BuddyInABox.TeamToRole[team];
			int num = 0;
			foreach (ReferenceHub referenceHub in WaveSpawner.GetAvailablePlayers(team))
			{
				if (num >= 2)
				{
					break;
				}
				referenceHub.roleManager.ServerSetRole(roleTypeId, RoleChangeReason.ItemUsage, ~RoleSpawnFlags.UseSpawnpoint);
				IFpcRole fpcRole2 = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole2 != null)
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
			WaveManager.OnWaveSpawned += delegate(SpawnableWaveBase w, List<ReferenceHub> _)
			{
				BuddyInABox._waveSpawnedAlready = true;
			};
			CustomNetworkManager.OnClientReady += delegate
			{
				BuddyInABox._waveSpawnedAlready = false;
			};
		}

		// Note: this type is marked as 'beforefieldinit'.
		static BuddyInABox()
		{
			Dictionary<Team, RoleTypeId> dictionary = new Dictionary<Team, RoleTypeId>();
			dictionary[Team.ClassD] = RoleTypeId.ChaosRifleman;
			dictionary[Team.Scientists] = RoleTypeId.NtfPrivate;
			BuddyInABox.TeamToRole = dictionary;
		}

		private const int MinimumSpectators = 2;

		private const float MaxSpawnDistance = 3f;

		private static readonly Dictionary<Team, RoleTypeId> TeamToRole;

		private static bool _waveSpawnedAlready;
	}
}
