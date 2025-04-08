using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using PlayerStatsSystem;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class HealCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "heal";

		public string[] Aliases { get; }

		public string Description { get; } = "Heals player(s) a specified amount.";

		public string[] Usage { get; } = new string[] { "%player%", "Amount (0 = full)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			int num = ((array != null && array.Length != 0 && int.TryParse(array[0], out num)) ? num : 0);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num2 = 0;
			if (list != null)
			{
				foreach (ReferenceHub referenceHub in list)
				{
					if (referenceHub != null)
					{
						HealthStat module = referenceHub.playerStats.GetModule<HealthStat>();
						if (num > 0)
						{
							module.ServerHeal((float)num);
						}
						else
						{
							module.CurValue = module.MaxValue;
						}
						if (num2 != 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
						num2++;
					}
				}
			}
			if (num2 > 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} healed player{1}{2}.", sender.LogName, (num2 == 1) ? " " : "s ", stringBuilder), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!");
			return true;
		}
	}
}
