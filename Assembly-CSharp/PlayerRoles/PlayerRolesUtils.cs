using System;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;

namespace PlayerRoles
{
	public static class PlayerRolesUtils
	{
		public static RoleTypeId GetRoleId(this ReferenceHub hub)
		{
			return hub.roleManager.CurrentRole.RoleTypeId;
		}

		public static Team GetTeam(this ReferenceHub hub)
		{
			return hub.roleManager.CurrentRole.Team;
		}

		public static Team GetTeam(this RoleTypeId role)
		{
			PlayerRoleBase playerRoleBase;
			if (!PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(role, out playerRoleBase))
			{
				return Team.OtherAlive;
			}
			return playerRoleBase.Team;
		}

		public static Faction GetFaction(this ReferenceHub hub)
		{
			return hub.GetTeam().GetFaction();
		}

		public static Faction GetFaction(this RoleTypeId role)
		{
			return role.GetTeam().GetFaction();
		}

		public static Faction GetFaction(this Team t)
		{
			switch (t)
			{
			case Team.SCPs:
				return Faction.SCP;
			case Team.FoundationForces:
			case Team.Scientists:
				return Faction.FoundationStaff;
			case Team.ChaosInsurgency:
			case Team.ClassD:
				return Faction.FoundationEnemy;
			case Team.Flamingos:
				return Faction.Flamingos;
			}
			return Faction.Unclassified;
		}

		public static bool IsHuman(this RoleTypeId role)
		{
			Team team = role.GetTeam();
			return team != Team.Dead && team != Team.SCPs && !role.IsFlamingo(true);
		}

		public static bool IsHuman(this ReferenceHub hub)
		{
			return hub.roleManager.CurrentRole.RoleTypeId.IsHuman();
		}

		public static bool IsFlamingo(this RoleTypeId role, bool ignoreScpTeam = true)
		{
			Team team = role.GetTeam();
			if (team != Team.SCPs)
			{
				return team == Team.Flamingos;
			}
			return role == RoleTypeId.ZombieFlamingo && !ignoreScpTeam;
		}

		public static bool IsFlamingo(this ReferenceHub hub, bool ignoreScpTeam = true)
		{
			return hub.roleManager.CurrentRole.RoleTypeId.IsFlamingo(ignoreScpTeam);
		}

		public static bool IsZombie(this RoleTypeId role)
		{
			return role == RoleTypeId.ZombieFlamingo || role == RoleTypeId.Scp0492;
		}

		public static bool IsAlive(this RoleTypeId role)
		{
			return role.GetTeam() != Team.Dead;
		}

		public static bool IsAlive(this ReferenceHub hub)
		{
			return hub.GetTeam() != Team.Dead;
		}

		public static bool IsSCP(this ReferenceHub hub, bool includeZombies = true)
		{
			PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
			return currentRole.Team == Team.SCPs && (includeZombies || !currentRole.RoleTypeId.IsZombie());
		}

		public static CharacterModel GetModel(this ReferenceHub hub)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return null;
			}
			return fpcRole.FpcModule.CharacterModelInstance;
		}

		public static void ForEachRole<T>(Action<ReferenceHub, T> action) where T : PlayerRoleBase
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				T t = referenceHub.roleManager.CurrentRole as T;
				if (t != null && action != null)
				{
					action(referenceHub, t);
				}
			}
		}

		public static void ForEachRole<T>(Action<T> action) where T : PlayerRoleBase
		{
			PlayerRolesUtils.ForEachRole<T>(delegate(ReferenceHub x, T y)
			{
				Action<T> action2 = action;
				if (action2 == null)
				{
					return;
				}
				action2(y);
			});
		}

		public static void ForEachRole<T>(Action<ReferenceHub> action) where T : PlayerRoleBase
		{
			PlayerRolesUtils.ForEachRole<T>(delegate(ReferenceHub x, T y)
			{
				Action<ReferenceHub> action2 = action;
				if (action2 == null)
				{
					return;
				}
				action2(x);
			});
		}

		public static string GetColoredName(this PlayerRoleBase role)
		{
			return string.Concat(new string[]
			{
				"<color=",
				role.RoleColor.ToHex(),
				">",
				role.RoleName,
				"</color>"
			});
		}

		public static readonly CachedLayerMask BlockerMask = new CachedLayerMask(new string[] { "Default", "Door", "Glass" });
	}
}
