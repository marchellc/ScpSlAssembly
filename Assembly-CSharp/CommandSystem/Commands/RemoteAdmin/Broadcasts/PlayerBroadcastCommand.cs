using System;
using System.Collections.Generic;
using System.Text;
using NorthwoodLib.Pools;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Broadcasts
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class PlayerBroadcastCommand : BroadcastCommandBase
	{
		public override string Command { get; } = "playerbroadcast";

		public override string[] Aliases { get; } = new string[] { "pbc" };

		public override string Description { get; } = "Sends an administrative broadcast to specific player(s).";

		public override string[] Usage { get; } = new string[] { "%player%", "Duration", "BroadcastFlag (Optional)", "Message" };

		public override bool OnExecute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (array == null || array.Length < this.MinimumArguments)
			{
				response = "To execute this command provide at least 3 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			string text = array[0];
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
			bool flag = base.HasInputFlag(array[1], out broadcastFlags, array.Length);
			string text2 = RAUtils.FormatArguments(array.Segment(1), flag ? 1 : 0);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			Broadcast singleton = Broadcast.Singleton;
			int num2 = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (num2++ != 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
				singleton.TargetAddElement(referenceHub.connectionToClient, text2, num, broadcastFlags);
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} broadcast text \"{1}\" to {2} players. Duration: {3} seconds. Affected players: {4}. Broadcast Flag: {5}.", new object[] { sender.LogName, text2, num2, num, stringBuilder, broadcastFlags }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Broadcast sent to {0} players.", num2);
			return true;
		}
	}
}
