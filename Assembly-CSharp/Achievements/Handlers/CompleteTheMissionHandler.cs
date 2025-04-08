using System;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class CompleteTheMissionHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			RoundSummary.OnRoundEnded += CompleteTheMissionHandler.OnRoundEnded;
		}

		private static void OnRoundEnded(RoundSummary.LeadingTeam leadingTeam, RoundSummary.SumInfo_ClassList sumInfo)
		{
			ReferenceHub referenceHub = null;
			int num = 0;
			foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
			{
				Team team = referenceHub2.GetTeam();
				if (team != Team.Dead && leadingTeam == CompleteTheMissionHandler.GetLeadingTeam(team))
				{
					num++;
					referenceHub = referenceHub2;
					if (num > 1)
					{
						break;
					}
				}
			}
			if (num == 1)
			{
				AchievementHandlerBase.ServerAchieve(referenceHub.connectionToClient, AchievementName.CompleteTheMission);
			}
		}

		private static RoundSummary.LeadingTeam GetLeadingTeam(Team team)
		{
			switch (team)
			{
			case Team.SCPs:
				return RoundSummary.LeadingTeam.Anomalies;
			case Team.FoundationForces:
			case Team.Scientists:
				return RoundSummary.LeadingTeam.FacilityForces;
			case Team.ChaosInsurgency:
			case Team.ClassD:
				return RoundSummary.LeadingTeam.ChaosInsurgency;
			default:
				return RoundSummary.LeadingTeam.Draw;
			}
		}
	}
}
