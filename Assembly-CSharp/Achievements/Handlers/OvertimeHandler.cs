using System;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class OvertimeHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			RoundSummary.OnRoundEnded += OvertimeHandler.OnRoundEnded;
		}

		private static void OnRoundEnded(RoundSummary.LeadingTeam leading, RoundSummary.SumInfo_ClassList sumInfo)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				PlayerRoleBase currentRole = referenceHub.roleManager.CurrentRole;
				if (currentRole == null || currentRole.RoleTypeId != RoleTypeId.FacilityGuard)
				{
					goto IL_0040;
				}
				RoleChangeReason serverSpawnReason = currentRole.ServerSpawnReason;
				if (serverSpawnReason - RoleChangeReason.RoundStart > 1)
				{
					goto IL_0040;
				}
				bool flag = true;
				IL_0043:
				if (flag)
				{
					AchievementHandlerBase.ServerAchieve(referenceHub.connectionToClient, AchievementName.Overtime);
					continue;
				}
				continue;
				IL_0040:
				flag = false;
				goto IL_0043;
			}
		}

		private const RoleTypeId TargetRole = RoleTypeId.FacilityGuard;
	}
}
