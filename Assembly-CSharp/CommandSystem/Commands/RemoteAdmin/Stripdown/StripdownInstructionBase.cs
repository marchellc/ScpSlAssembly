using System;
using System.Text;
using Mirror;
using RemoteAdmin;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown
{
	[CommandHandler(typeof(StripdownCommand))]
	public abstract class StripdownInstructionBase : ICommand
	{
		public abstract string Command { get; }

		public string[] Aliases
		{
			get
			{
				return null;
			}
		}

		public abstract string Description { get; }

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			NetworkConnection networkConnection;
			if (playerCommandSender != null)
			{
				networkConnection = playerCommandSender.ReferenceHub.connectionToClient;
			}
			else
			{
				ReferenceHub referenceHub;
				if (!(sender is ServerConsoleSender) || !ReferenceHub.TryGetLocalHub(out referenceHub))
				{
					response = "No valid receiver found.";
					return false;
				}
				networkConnection = referenceHub.connectionToClient;
			}
			StripdownInstructionBase.Combiner.Clear();
			bool flag = false;
			for (int i = 0; i < arguments.Count; i++)
			{
				if (flag)
				{
					StripdownInstructionBase.Combiner.Append(' ');
				}
				else
				{
					flag = true;
				}
				StripdownInstructionBase.Combiner.Append(arguments.Array[arguments.Offset + i]);
			}
			response = this.ProcessInstruction(networkConnection, StripdownInstructionBase.Combiner.ToString());
			return true;
		}

		protected abstract string ProcessInstruction(NetworkConnection conn, string instruction);

		private static readonly StringBuilder Combiner = new StringBuilder();
	}
}
