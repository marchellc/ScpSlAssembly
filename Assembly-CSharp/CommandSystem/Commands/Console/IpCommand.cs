using System;
using System.Net;

namespace CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class IpCommand : ICommand
	{
		public string Command { get; } = "ip";

		public string[] Aliases { get; } = new string[] { "whatismyip", "myip" };

		public string Description { get; } = "Returns the IP used when connecting to central servers";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			bool flag;
			HttpStatusCode httpStatusCode;
			string text = HttpQuery.Get(CentralServer.StandardUrl + "ip.php", out flag, out httpStatusCode);
			if (!flag)
			{
				response = string.Format("HTTP request failed: {0}", httpStatusCode);
				return false;
			}
			response = "IP: " + (text.EndsWith(".") ? text.Remove(text.Length - 1) : text);
			return true;
		}
	}
}
