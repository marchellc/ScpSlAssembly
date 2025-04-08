using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using InventorySystem.Disarming;
using InventorySystem.Items;
using Mirror;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class EscapeHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			Escape.OnServerPlayerEscape += EscapeHandler.OnEscaped;
			PlayerRoleManager.OnServerRoleSet += EscapeHandler.OnRoleSet;
		}

		private static void OnRoleSet(ReferenceHub userHub, RoleTypeId newId, RoleChangeReason reason)
		{
			if (!NetworkServer.active || EscapeHandler._escapeFired || reason != RoleChangeReason.Escaped)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(userHub.networkIdentity.connectionToClient, AchievementName.EscapeArtist);
			EscapeHandler._escapeFired = true;
		}

		internal override void OnRoundStarted()
		{
			EscapeHandler._escapeFired = false;
		}

		private static void OnEscaped(ReferenceHub userHub)
		{
			NetworkConnectionToClient connectionToClient = userHub.networkIdentity.connectionToClient;
			PlayerRoleBase currentRole = userHub.roleManager.CurrentRole;
			if (userHub.playerEffectsController.GetEffect<Scp207>().IsEnabled)
			{
				AchievementHandlerBase.ServerAchieve(connectionToClient, AchievementName.Escape207);
			}
			if (currentRole.RoleTypeId == RoleTypeId.Scientist)
			{
				AchievementHandlerBase.ServerAchieve(connectionToClient, AchievementName.ForScience);
				return;
			}
			if (currentRole.RoleTypeId == RoleTypeId.ClassD)
			{
				AchievementHandlerBase.ServerAchieve(connectionToClient, AchievementName.ItsAlwaysLeft);
				int num = 0;
				if (!userHub.inventory.IsDisarmed())
				{
					using (Dictionary<ushort, ItemBase>.ValueCollection.Enumerator enumerator = userHub.inventory.UserInventory.Items.Values.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.Category == ItemCategory.SCPItem)
							{
								num++;
							}
						}
					}
				}
				if (num >= 2)
				{
					AchievementHandlerBase.ServerAchieve(connectionToClient, AchievementName.PropertyOfChaos);
				}
			}
		}

		private static bool _escapeFired;
	}
}
