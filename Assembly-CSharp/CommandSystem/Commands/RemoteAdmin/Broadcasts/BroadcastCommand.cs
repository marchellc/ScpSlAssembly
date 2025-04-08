using System;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Broadcasts
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class BroadcastCommand : BroadcastCommandBase
	{
		public override string Command { get; } = "broadcast";

		public override string[] Aliases { get; } = new string[] { "bc", "alert" };

		public override string Description { get; } = "Sends an administrative broadcast to all players.";

		public override string[] Usage { get; } = new string[] { "Duration", "BroadcastFlag (Optional)", "Message" };

		public override bool OnExecute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			string text = arguments.At(0);
			ushort num;
			if (!base.IsValidDuration(text, out num))
			{
				response = string.Concat(new string[]
				{
					"Invalid argument for duration: ",
					text,
					" Usage: ",
					arguments.Array[0],
					" ",
					this.DisplayCommandUsage()
				});
				return false;
			}
			Broadcast.BroadcastFlags broadcastFlags;
			bool flag = base.HasInputFlag(arguments.At(1), out broadcastFlags, arguments.Count);
			string text2 = RAUtils.FormatArguments(arguments, flag ? 2 : 1);
			Broadcast.Singleton.RpcAddElement(text2, num, broadcastFlags);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} broadcast text \"{1}\". Duration: {2} seconds. Broadcast Flag: {3}.", new object[] { sender.LogName, text2, text, broadcastFlags }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = "Global broadcast sent.";
			return true;
		}
	}
}
