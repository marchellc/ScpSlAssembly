using System;
using System.Collections.Generic;
using System.Text;
using Interactables.Interobjects.DoorUtils;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Doors
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class DoorTPCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "doortp";

		public string[] Aliases { get; } = new string[] { "dtp", "doorteleport" };

		public string Description { get; } = "Teleports player(s) to the specified door.";

		public string[] Usage { get; } = new string[] { "%player%", "%door%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.PlayersManagement, out response))
			{
				return false;
			}
			if (arguments.Count < 2)
			{
				response = string.Concat(new string[]
				{
					"To execute this command provide at least 2 arguments!\nUsage: ",
					arguments.Array[0],
					" ",
					this.DisplayCommandUsage(),
					" [Door name]"
				});
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (array == null || string.IsNullOrEmpty(array[0]))
			{
				response = "Invalid door name provided.";
				return false;
			}
			string text = array[0].Split('.', StringSplitOptions.None)[0].ToUpper();
			DoorNametagExtension doorNametagExtension;
			if (!DoorNametagExtension.NamedDoors.TryGetValue(text, out doorNametagExtension))
			{
				response = "Can't find door " + text + ".";
				return false;
			}
			Vector3 vector = DoorTPCommand.EnsurePositionSafety(doorNametagExtension.transform);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			int num = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (referenceHub.TryOverridePosition(vector))
				{
					if (num != 0)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(referenceHub.LoggedNameFromRefHub());
					num++;
				}
			}
			if (num > 0)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} teleported player{1}{2} to door {3}.", new object[]
				{
					sender.LogName,
					(num == 1) ? " " : "s ",
					stringBuilder,
					array[0].TrimEnd('.')
				}), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			}
			StringBuilderPool.Shared.Return(stringBuilder);
			response = string.Format("Done! The request affected {0} player{1}", num, (num == 1) ? "!" : "s!");
			return true;
		}

		public static Vector3 EnsurePositionSafety(Transform doorTransform)
		{
			Vector3 vector = doorTransform.position + Vector3.up;
			while (Physics.CheckSphere(vector, 0.35f, FpcStateProcessor.Mask))
			{
				vector += doorTransform.forward * 0.35f;
			}
			return vector;
		}

		private const float WallDetectionRadius = 0.35f;
	}
}
