using System;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(ElevatorCommand))]
	public class ElevatorUnlockCommand : ICommand
	{
		public string Command { get; } = "unlock";

		public string[] Aliases { get; } = new string[] { "u", "ul", "ulck" };

		public string Description { get; } = "Unlocks an elevator.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			int count = arguments.Count;
			if (count != 1)
			{
				if (count == 2)
				{
					int num;
					if (int.TryParse(arguments.At(1), out num) && num >= 0)
					{
						return ElevatorLockCommand.TrySetLock(arguments.At(0), false, out response, sender, num);
					}
				}
				response = "Syntax error: elevator unlock <Elevator ID / \"all\"> <level (optional)>";
				return false;
			}
			return ElevatorLockCommand.TrySetLock(arguments.At(0), false, out response, sender, -1);
		}
	}
}
