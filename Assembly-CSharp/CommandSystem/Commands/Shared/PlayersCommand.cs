using System;
using System.Collections.Generic;
using System.Text;
using CentralAuth;
using Mirror;
using NorthwoodLib.Pools;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class PlayersCommand : ICommand
	{
		public string Command { get; } = "players";

		public string[] Aliases { get; } = new string[] { "pl", "list" };

		public string Description { get; } = "Displays a list of all players.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (NetworkServer.active && !sender.CheckPermission(PlayerPermissions.PlayerSensitiveDataAccess, out response))
			{
				return false;
			}
			if (ReferenceHub.LocalHub == null)
			{
				response = "You must join a server to execute this command.";
				return false;
			}
			HashSet<ReferenceHub> allHubs = ReferenceHub.AllHubs;
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			bool flag;
			try
			{
				stringBuilder.AppendFormat("<color=cyan>List of players ({0}):</color>\n", ServerStatic.IsDedicated ? (allHubs.Count - 1) : allHubs.Count);
				foreach (ReferenceHub referenceHub in allHubs)
				{
					ClientInstanceMode mode = referenceHub.Mode;
					if (mode != ClientInstanceMode.DedicatedServer && mode != ClientInstanceMode.Dummy)
					{
						string text = referenceHub.authManager.UserId;
						if (text == null)
						{
							text = "(no UserID)";
						}
						stringBuilder.AppendFormat("- {0}: {1} [{2}]\n", referenceHub.nicknameSync.CombinedName ?? "(no nickname)", text, referenceHub.PlayerId);
					}
				}
				response = stringBuilder.ToString();
				flag = true;
			}
			finally
			{
				StringBuilderPool.Shared.Return(stringBuilder);
			}
			return flag;
		}
	}
}
