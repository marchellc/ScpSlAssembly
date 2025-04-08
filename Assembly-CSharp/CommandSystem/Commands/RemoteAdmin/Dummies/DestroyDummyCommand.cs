using System;
using System.Collections.Generic;
using GameCore;
using Mirror;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Dummies
{
	[CommandHandler(typeof(DummiesCommand))]
	public class DestroyDummyCommand : ICommand
	{
		public string Command { get; } = "destroy";

		public string[] Aliases { get; } = new string[] { "d", "kill", "remove" };

		public string Description { get; } = "Spawns a dummy on the map.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "You must specify a dummy to destroy.";
				return false;
			}
			if (arguments.At(0).Equals("all", StringComparison.OrdinalIgnoreCase))
			{
				DummyUtils.DestroyAllDummies();
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " has destroyed all dummies.", ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
				response = "All dummies have been destroyed.";
				return true;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (list == null)
			{
				response = "An unexpected problem has occurred during PlayerId or name array processing.";
				return false;
			}
			int num = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (!(referenceHub == null) && referenceHub.IsDummy)
				{
					NetworkServer.Destroy(referenceHub.gameObject);
					num++;
				}
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} administratively destroyed {1} dummies.", sender.LogName, num), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = string.Format("Done! The request affected {0} dumm{1}", num, (num == 1) ? "y!" : "ies!");
			return true;
		}
	}
}
