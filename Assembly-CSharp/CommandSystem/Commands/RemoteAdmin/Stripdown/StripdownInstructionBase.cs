using System;
using System.Text;
using Mirror;
using RemoteAdmin;

namespace CommandSystem.Commands.RemoteAdmin.Stripdown;

[CommandHandler(typeof(StripdownCommand))]
public abstract class StripdownInstructionBase : ICommand
{
	private static readonly StringBuilder Combiner = new StringBuilder();

	public abstract string Command { get; }

	public string[] Aliases => null;

	public abstract string Description { get; }

	public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
	{
		if (!sender.CheckPermission(PlayerPermissions.PermissionsManagement, out response))
		{
			return false;
		}
		NetworkConnection connectionToClient;
		if (sender is PlayerCommandSender playerCommandSender)
		{
			connectionToClient = playerCommandSender.ReferenceHub.connectionToClient;
		}
		else
		{
			if (!(sender is ServerConsoleSender) || !ReferenceHub.TryGetLocalHub(out var hub))
			{
				response = "No valid receiver found.";
				return false;
			}
			connectionToClient = hub.connectionToClient;
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
		response = this.ProcessInstruction(connectionToClient, StripdownInstructionBase.Combiner.ToString());
		return true;
	}

	protected abstract string ProcessInstruction(NetworkConnection conn, string instruction);
}
