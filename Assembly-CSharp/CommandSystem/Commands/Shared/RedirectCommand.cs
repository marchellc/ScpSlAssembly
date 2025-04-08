using System;
using System.Collections.Generic;
using MEC;
using RoundRestarting;
using ServerOutput;
using Utils.Networking;

namespace CommandSystem.Commands.Shared
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class RedirectCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "redirect";

		public string[] Aliases { get; } = new string[] { "rstop", "rexit", "rquit" };

		public string Description { get; } = "Shutdowns the server and redirects all the players to a server on another port.";

		public string[] Usage { get; } = new string[] { "port number" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			ushort num;
			if (arguments.Count != 1 || !ushort.TryParse(arguments.At(0), out num))
			{
				response = "First argument must be a valid port number.";
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} redirected all players to port {1}.", sender.LogName, num), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			CustomLiteNetLib4MirrorTransport.DelayConnections = true;
			IdleMode.SetIdleMode(false);
			IdleMode.PauseIdleMode = true;
			ServerConsole.AddOutputEntry(default(ExitActionShutdownEntry));
			new RoundRestartMessage(RoundRestartType.RedirectRestart, 0.1f, num, true, false).SendToAuthenticated(0);
			Timing.RunCoroutine(this.ScheduleShutdown(), Segment.FixedUpdate);
			response = string.Format("Players have been redirected to port {0}. Server will shutdown in a couple of seconds.", num);
			return true;
		}

		private IEnumerator<float> ScheduleShutdown()
		{
			yield return Timing.WaitForSeconds(5f);
			Shutdown.Quit(true, false);
			yield break;
		}
	}
}
