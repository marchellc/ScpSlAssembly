using System;
using System.Collections.Generic;
using InventorySystem.Disarming;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class JustResourcesHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += this.HandleDeath;
			DisarmingHandlers.OnPlayerDisarmed += this.HandleDisarmed;
		}

		internal override void OnRoundStarted()
		{
			this._interactedPlayers.Clear();
		}

		private void HandleDisarmed(ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (disarmerHub == null || targetHub == null)
			{
				return;
			}
			if (disarmerHub.GetTeam() != Team.Scientists)
			{
				return;
			}
			if (targetHub.GetTeam() != Team.ClassD)
			{
				return;
			}
			if (this._interactedPlayers.Contains(targetHub))
			{
				return;
			}
			this._interactedPlayers.Add(targetHub);
			AchievementHandlerBase.ServerAchieve(disarmerHub.networkIdentity.connectionToClient, AchievementName.JustResources);
		}

		private void HandleDeath(ReferenceHub deadPlayer, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null || attackerDamageHandler.Attacker.Hub == null)
			{
				return;
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (hub.GetTeam() != Team.Scientists)
			{
				return;
			}
			if (deadPlayer.GetTeam() != Team.ClassD)
			{
				return;
			}
			if (this._interactedPlayers.Contains(deadPlayer))
			{
				return;
			}
			this._interactedPlayers.Add(hub);
			AchievementHandlerBase.ServerAchieve(hub.networkIdentity.connectionToClient, AchievementName.JustResources);
		}

		private List<ReferenceHub> _interactedPlayers = new List<ReferenceHub>();
	}
}
