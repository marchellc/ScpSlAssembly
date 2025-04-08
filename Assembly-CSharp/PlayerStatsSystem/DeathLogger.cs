using System;
using System.Text;
using NorthwoodLib.Pools;
using PlayerRoles;
using UnityEngine;

namespace PlayerStatsSystem
{
	public static class DeathLogger
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerStats.OnAnyPlayerDied += DeathLogger.HandleDeath;
		}

		private static void HandleDeath(ReferenceHub ply, DamageHandlerBase handler)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.Append(ply.LoggedNameFromRefHub());
			stringBuilder.Append(", playing as ");
			stringBuilder.Append(ply.roleManager.CurrentRole.RoleName);
			stringBuilder.Append(", ");
			AttackerDamageHandler attackerDamageHandler = handler as AttackerDamageHandler;
			ServerLogs.ServerLogType serverLogType;
			if (attackerDamageHandler != null)
			{
				if (attackerDamageHandler.IsSuicide)
				{
					serverLogType = ServerLogs.ServerLogType.Suicide;
					stringBuilder.Append("has commited suicide.");
				}
				else
				{
					if (attackerDamageHandler.IsFriendlyFire)
					{
						serverLogType = ServerLogs.ServerLogType.Teamkill;
						stringBuilder.Append("has been teamkilled by ");
					}
					else
					{
						serverLogType = ServerLogs.ServerLogType.KillLog;
						stringBuilder.Append("has been killed by ");
					}
					stringBuilder.Append(attackerDamageHandler.Attacker.Nickname);
					stringBuilder.Append(" (");
					stringBuilder.Append(attackerDamageHandler.Attacker.LogUserID);
					stringBuilder.Append(") playing as: ");
					PlayerRoleBase playerRoleBase;
					stringBuilder.Append(PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(attackerDamageHandler.Attacker.Role, out playerRoleBase) ? playerRoleBase.RoleName : "Unknown class");
					stringBuilder.Append(".");
				}
			}
			else
			{
				serverLogType = ServerLogs.ServerLogType.KillLog;
				stringBuilder.Append("has died.");
			}
			stringBuilder.Append(" Specific death reason: ");
			string serverLogsText = handler.ServerLogsText;
			stringBuilder.Append(serverLogsText);
			if (!serverLogsText.EndsWith(".", StringComparison.Ordinal))
			{
				stringBuilder.Append(".");
			}
			ServerLogs.AddLog(ServerLogs.Modules.ClassChange, stringBuilder.ToString(), serverLogType, false);
			StringBuilderPool.Shared.Return(stringBuilder);
		}
	}
}
