using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using RemoteAdmin;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class BringCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "bring";

		public string[] Aliases { get; }

		public string Description { get; } = "Brings the specified player(s) to your position.";

		public string[] Usage { get; } = new string[] { "%player%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "You must be in-game to use this command!";
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			if (!playerCommandSender.ReferenceHub.IsAlive())
			{
				response = "Command disabled when you are not alive!";
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			Vector3 position = playerCommandSender.ReferenceHub.transform.position;
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (referenceHub.IsAlive() && referenceHub.TryOverridePosition(position))
				{
					if (num != 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
					num++;
				}
			}
			if (num > 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} brought player{1}{2}.", sender.LogName, (num == 1) ? " " : "s ", stringBuilder), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
			return true;
		}
	}
}
