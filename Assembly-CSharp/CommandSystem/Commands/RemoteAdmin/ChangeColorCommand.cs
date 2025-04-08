using System;
using MapGeneration;
using PlayerRoles;
using RemoteAdmin;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ChangeColorCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "changecolor";

		public string[] Aliases { get; } = new string[] { "changec", "ccolor" };

		public string Description { get; } = "Changes the color of the lights in the room you are currently in.";

		public string[] Usage { get; } = new string[] { "r", "g", "b" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "You must be in-game to use this command!";
				return false;
			}
			if (!playerCommandSender.ReferenceHub.IsAlive())
			{
				response = "You need to be alive to run this command!";
				return false;
			}
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(playerCommandSender.ReferenceHub.transform.position);
			if (roomIdentifier == null)
			{
				response = "You are not in a a room!";
				return false;
			}
			RoomLightController closestLightController = roomIdentifier.GetClosestLightController(playerCommandSender.ReferenceHub);
			if (closestLightController == null)
			{
				response = "You are not in a room that supports changing lights color!";
				return false;
			}
			if (arguments.Count == 0)
			{
				closestLightController.NetworkOverrideColor = Color.clear;
				response = "Done! Reset warhead lights to default color.";
				return true;
			}
			if (arguments.Count < 3)
			{
				response = "Type 3 numbers, eg: 255 255 255";
				return false;
			}
			float num;
			float num2;
			float num3;
			if (!float.TryParse(arguments.At(0), out num) || !float.TryParse(arguments.At(1), out num2) || !float.TryParse(arguments.At(2), out num3))
			{
				response = "Invalid input. Type 3 numbers, eg: 255 255 255";
				return false;
			}
			closestLightController.NetworkOverrideColor = new Color(num / 255f, num2 / 255f, num3 / 255f);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} changed color of lights in a room to {1} {2} {3} .", new object[] { sender.LogName, num, num2, num3 }), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			response = "Done!";
			return true;
		}
	}
}
