using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class EffectCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "effect";

		public string[] Aliases { get; } = new string[] { "pfx" };

		public string Description { get; } = "Applies a status effect to player(s).";

		public string[] Usage { get; } = new string[] { "EffectName", "Intensity", "Duration", "%player%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Effects, out response))
			{
				return false;
			}
			if (arguments.Count < 4)
			{
				response = "To execute this command provide at least 4 arguments!\nUsage: " + this.Command + " " + this.DisplayCommandUsage();
				return false;
			}
			string text = arguments.At(0);
			byte b;
			if (!byte.TryParse(arguments.At(1), out b))
			{
				response = string.Concat(new string[]
				{
					"Effect intensity must be a byte value between 0-255.\nUsage: ",
					this.Command,
					" ",
					this.DisplayCommandUsage(),
					"'"
				});
				return false;
			}
			float num;
			if (!float.TryParse(arguments.At(2), out num))
			{
				response = string.Concat(new string[]
				{
					"Effect duration must be a valid float value.\nUsage: ",
					this.Command,
					" ",
					this.DisplayCommandUsage(),
					"'"
				});
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 3, out array, false);
			if (list == null || list.Count == 0)
			{
				response = "Couldn't find any player(s) using the specified arguments.";
				return false;
			}
			int num2 = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				StatusEffectBase statusEffectBase;
				if (!(referenceHub == null) && referenceHub.playerEffectsController.TryGetEffect(text, out statusEffectBase))
				{
					statusEffectBase.ServerSetState(b, num, false);
					ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} applied a status effect {1} for player {2}. Intensity: {3} - Duration: {4}.", new object[]
					{
						sender.LogName,
						text,
						referenceHub.LoggedNameFromRefHub(),
						b,
						num
					}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					num2++;
				}
			}
			response = string.Format("Done! The request affected {0} player{1}", num2, (num2 == 1) ? "!" : "s!");
			return true;
		}
	}
}
