using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using PlayerStatsSystem;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class KillCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "kill";

		public string[] Aliases { get; }

		public string Description { get; } = "Kills the specified player(s).";

		public string[] Usage { get; } = new string[] { "PlayerId" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = string.Format("To execute this command provide at least 1 argument!\nUsage: {0} {1}", arguments.Array[0], this.Usage);
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num = 0;
			if (list != null)
			{
				foreach (ReferenceHub referenceHub in list)
				{
					if (referenceHub != null && referenceHub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.Unknown, null)))
					{
						if (num != 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
						num++;
					}
				}
			}
			if (num > 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} administratively killed player{1}{2}.", sender.LogName, (num == 1) ? " " : "s ", stringBuilder), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
			return true;
		}
	}
}
