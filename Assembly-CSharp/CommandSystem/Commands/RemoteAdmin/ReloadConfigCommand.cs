using System;
using GameCore;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ReloadConfigCommand : ICommand
	{
		public string Command { get; } = "reloadconfig";

		public string[] Aliases { get; } = new string[] { "rc" };

		public string Description { get; } = "Reloads all configs.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.ServerConfigs, out response))
			{
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " reloaded configuration files.", ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
			bool flag;
			try
			{
				ConfigFile.ReloadGameConfigs(false);
				response = "Reloaded all configs!";
				flag = true;
			}
			catch (Exception ex)
			{
				response = string.Format("Reloading configs failed: {0}", ex);
				flag = false;
			}
			return flag;
		}
	}
}
