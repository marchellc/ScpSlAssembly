using System;
using System.Collections.Generic;
using RemoteAdmin;
using UnityEngine;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin.Dummies
{
	[CommandHandler(typeof(DummiesCommand))]
	public class FollowDummyCommand : ICommand
	{
		public string Command { get; } = "follow";

		public string[] Aliases { get; } = new string[] { "f", "trail" };

		public string Description { get; } = "Makes a dummy un/follow you.";

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "This command can only be executed by a physical player.";
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "You must specify a dummy to destroy.";
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			if (list == null)
			{
				response = "An unexpected problem has occurred during PlayerId or name array processing.";
				return false;
			}
			int num = 0;
			foreach (ReferenceHub referenceHub in list)
			{
				if (!(referenceHub == null) && referenceHub.IsDummy)
				{
					PlayerFollower playerFollower;
					if (referenceHub.TryGetComponent<PlayerFollower>(out playerFollower))
					{
						global::UnityEngine.Object.Destroy(playerFollower);
					}
					else
					{
						referenceHub.gameObject.AddComponent<PlayerFollower>().Init(playerCommandSender.ReferenceHub, 20f, 1.75f, 30f);
					}
					num++;
				}
			}
			response = string.Format("Done! The request affected {0} dumm{1}", num, (num == 1) ? "y!" : "ies!");
			return true;
		}
	}
}
