using System;
using InventorySystem.Items.MicroHID;
using InventorySystem.Items.MicroHID.Modules;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp106;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class IllPassThanksHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDied += this.HandleDeath;
			Scp106Attack.OnPlayerTeleported += this.PlayerTeleported;
		}

		private void PlayerTeleported(ReferenceHub scp106, ReferenceHub hub)
		{
			if (!this.CheckConditionsForVictim(hub))
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(scp106.connectionToClient, AchievementName.IllPassThanks);
		}

		private void HandleDeath(ReferenceHub deadPlayer, DamageHandlerBase handler)
		{
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			if (attackerDamageHandler == null)
			{
				return;
			}
			ReferenceHub hub = attackerDamageHandler.Attacker.Hub;
			if (hub == null || !hub.IsSCP(true) || !this.CheckConditionsForVictim(deadPlayer))
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.IllPassThanks);
		}

		private bool CheckConditionsForVictim(ReferenceHub hub)
		{
			MicroHIDItem microHIDItem = hub.inventory.CurInstance as MicroHIDItem;
			if (microHIDItem != null && microHIDItem != null)
			{
				MicroHidPhase phase = microHIDItem.CycleController.Phase;
				return phase == MicroHidPhase.WindingUp || phase == MicroHidPhase.Firing;
			}
			return false;
		}
	}
}
