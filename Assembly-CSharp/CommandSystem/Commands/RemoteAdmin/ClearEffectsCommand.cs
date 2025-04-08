using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ClearEffectsCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "cleareffects";

		public string[] Aliases { get; } = new string[] { "cfx", "clearfx" };

		public string Description { get; } = "Clears all status effects from the specified player(s).";

		public string[] Usage { get; } = new string[] { "%player%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Effects, out response))
			{
				return false;
			}
			if (arguments.Count == 0)
			{
				response = "To execute this command provide at least 1 arguments!\nUsage: " + this.Command + " " + string.Join(" ", this.Usage);
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			int num = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (!(referenceHub == null))
				{
					StatusEffectBase[] allEffects = referenceHub.playerEffectsController.AllEffects;
					for (int i = 0; i < allEffects.Length; i++)
					{
						allEffects[i].Intensity = 0;
					}
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + " clear all effects for player " + referenceHub.LoggedNameFromRefHub() + ".", ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					num++;
				}
			}
			response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
			return true;
		}
	}
}
