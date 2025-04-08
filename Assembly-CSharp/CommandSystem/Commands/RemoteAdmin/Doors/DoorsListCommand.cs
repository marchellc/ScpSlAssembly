using System;
using System.Collections.Generic;
using System.Linq;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;

namespace CommandSystem.Commands.RemoteAdmin.Doors
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class DoorsListCommand : ICommand
	{
		public string Command { get; } = "doorslist";

		public string[] Aliases { get; } = new string[] { "doors", "dl" };

		public string Description { get; } = "Lists all valid door names.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			string text = "List of named doors in the facility:\n";
			List<string> list = (from item in DoorNametagExtension.NamedDoors.Values.ToArray<DoorNametagExtension>()
				where !string.IsNullOrEmpty(item.GetName)
				select item).Select(delegate(DoorNametagExtension item)
			{
				string[] array = new string[5];
				array[0] = item.GetName;
				array[1] = " - ";
				array[2] = (item.TargetDoor.TargetState ? "<color=green>OPENED</color>" : "<color=orange>CLOSED</color>");
				array[3] = ((item.TargetDoor.ActiveLocks > 0) ? " <color=red>[LOCKED]</color>" : "");
				int num = 4;
				BasicDoor basicDoor = item.TargetDoor as BasicDoor;
				string text2;
				if (basicDoor == null || basicDoor.RequiredPermissions.RequiredPermissions <= KeycardPermissions.None)
				{
					CheckpointDoor checkpointDoor = item.TargetDoor as CheckpointDoor;
					if (checkpointDoor == null || checkpointDoor.RequiredPermissions.RequiredPermissions <= KeycardPermissions.None)
					{
						text2 = "";
						goto IL_0098;
					}
				}
				text2 = " <color=blue>[CARD REQUIRED]</color>";
				IL_0098:
				array[num] = text2;
				return string.Concat(array);
			}).ToList<string>();
			list.Sort();
			text += list.Aggregate((string current, string adding) => current + "\n" + adding);
			response = text;
			return true;
		}
	}
}
