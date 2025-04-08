using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Utils.Networking;

namespace Respawning.NamingRules
{
	public static class NamingRulesManager
	{
		public static event Action<Team, string, int> OnNameAdded;

		public static bool TryGetNamingRule(Team team, out UnitNamingRule rule)
		{
			return NamingRulesManager.AllNamingRules.TryGetValue(team, out rule);
		}

		public static string ClientFetchReceived(Team teamType, int unitNameId)
		{
			List<string> list;
			if (!NamingRulesManager.GeneratedNames.TryGetValue(teamType, out list))
			{
				return string.Empty;
			}
			int count = list.Count;
			if (count != 0 && unitNameId < count)
			{
				return list[unitNameId];
			}
			return string.Empty;
		}

		public static void ServerGenerateName(Team team, UnitNamingRule rule)
		{
			rule.GenerateNew();
			new UnitNameMessage
			{
				Team = team,
				NamingRule = rule
			}.SendToHubsConditionally((ReferenceHub x) => true, 0);
		}

		private static void ProcessMessage(UnitNameMessage msg)
		{
			List<string> list;
			if (NamingRulesManager.GeneratedNames.TryGetValue(msg.Team, out list))
			{
				list.Add(msg.UnitName);
			}
			else
			{
				list = new List<string> { msg.UnitName };
				NamingRulesManager.GeneratedNames.Add(msg.Team, list);
			}
			int num = list.Count - 1;
			Action<Team, string, int> onNameAdded = NamingRulesManager.OnNameAdded;
			if (onNameAdded == null)
			{
				return;
			}
			onNameAdded(msg.Team, msg.UnitName, num);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				UnitNameMessageHandler.ResetHistory();
				NamingRulesManager.GeneratedNames.Clear();
				NetworkClient.ReplaceHandler<UnitNameMessage>(new Action<UnitNameMessage>(NamingRulesManager.ProcessMessage), true);
				if (!NetworkServer.active)
				{
					return;
				}
				foreach (Team team in NamingRulesManager.PregeneratedNameTeams)
				{
					UnitNamingRule unitNamingRule;
					if (NamingRulesManager.TryGetNamingRule(team, out unitNamingRule))
					{
						NamingRulesManager.ServerGenerateName(team, unitNamingRule);
					}
				}
			};
		}

		// Note: this type is marked as 'beforefieldinit'.
		static NamingRulesManager()
		{
			Dictionary<Team, UnitNamingRule> dictionary = new Dictionary<Team, UnitNamingRule>();
			dictionary[Team.FoundationForces] = new NineTailedFoxNamingRule();
			NamingRulesManager.AllNamingRules = dictionary;
			NamingRulesManager.PregeneratedNameTeams = new Team[] { Team.FoundationForces };
		}

		public static Dictionary<Team, List<string>> GeneratedNames = new Dictionary<Team, List<string>>();

		private static readonly Dictionary<Team, UnitNamingRule> AllNamingRules;

		private static readonly Team[] PregeneratedNameTeams;
	}
}
