using System;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Broadcasts
{
	public abstract class BroadcastCommandBase : ICommand, IUsageProvider
	{
		public abstract string Command { get; }

		public abstract string[] Aliases { get; }

		public abstract string Description { get; }

		public abstract string[] Usage { get; }

		public virtual int MinimumArguments
		{
			get
			{
				return 2;
			}
		}

		public virtual bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.Broadcasting, out response))
			{
				return false;
			}
			if (arguments.Count < this.MinimumArguments)
			{
				response = string.Format("To execute this command provide at least {0} arguments!\nUsage: {1} {2}", this.MinimumArguments, arguments.Array[0], this.DisplayCommandUsage());
				return false;
			}
			return this.OnExecute(arguments, sender, out response);
		}

		public abstract bool OnExecute(ArraySegment<string> arguments, ICommandSender sender, out string response);

		protected bool HasInputFlag(string inputFlag, out Broadcast.BroadcastFlags broadcastFlag, int argumentCount = 0)
		{
			bool flag = RAUtils.IsDigit.IsMatch(inputFlag);
			broadcastFlag = Broadcast.BroadcastFlags.Normal;
			return !flag && argumentCount >= this.MinimumArguments + 1 && Enum.TryParse<Broadcast.BroadcastFlags>(inputFlag, true, out broadcastFlag);
		}

		protected bool IsValidDuration(string inputDuration, out ushort time)
		{
			return ushort.TryParse(inputDuration, out time) && time > 0;
		}
	}
}
