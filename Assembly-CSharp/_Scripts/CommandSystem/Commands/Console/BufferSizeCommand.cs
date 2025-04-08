using System;
using CommandSystem;
using ServerOutput;

namespace _Scripts.CommandSystem.Commands.Console
{
	[CommandHandler(typeof(GameConsoleCommandHandler))]
	public class BufferSizeCommand : ICommand
	{
		public string Command { get; } = "buffer";

		public string[] Aliases { get; } = new string[] { "bs" };

		public string Description { get; } = "Returns TcpConsole buffers sizes.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.ServerConsoleCommands, out response))
			{
				return false;
			}
			TcpConsole tcpConsole = ServerStatic.ServerOutput as TcpConsole;
			if (tcpConsole == null)
			{
				response = "Current ServerOutput is not TcpConsole!";
				return false;
			}
			response = string.Format("RX Buffer Size: {0}\nTX Buffer Size: {1}\n\nSpecified RX Buffer Size: {2}\nSpecified TX Buffer Size: {3}\n\nDefault RX Buffer Size: {4}\nDefault TX Buffer Size: {5}", new object[] { tcpConsole.ReceiveBufferSize, tcpConsole.SendBufferSize, tcpConsole.SpecifiedReceiveBufferSize, tcpConsole.SpecifiedSendBufferSize, 25000, 200000 });
			return true;
		}
	}
}
