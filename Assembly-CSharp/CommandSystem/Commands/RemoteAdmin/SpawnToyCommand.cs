using System;
using System.Collections.Generic;
using AdminToys;
using Mirror;
using RemoteAdmin;
using UnityEngine;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class SpawnToyCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "spawntoy";

		public string[] Aliases { get; }

		public string Description { get; } = "Spawns an admin toy.";

		public string[] Usage { get; } = new string[] { "Target toy (Optional, will list all available toys if not provided)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "Only players can run this command.";
				return false;
			}
			string text = ((arguments.Count < 1) ? "list" : arguments.Array[1]);
			bool flag = text == "list";
			if (flag)
			{
				response = "List of toys:";
			}
			using (Dictionary<uint, GameObject>.ValueCollection.Enumerator enumerator = NetworkClient.prefabs.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					AdminToyBase adminToyBase;
					if (enumerator.Current.TryGetComponent<AdminToyBase>(out adminToyBase))
					{
						if (flag)
						{
							response = response + "\n- " + adminToyBase.CommandName;
						}
						else if (string.Equals(text, adminToyBase.CommandName, StringComparison.InvariantCultureIgnoreCase))
						{
							AdminToyBase adminToyBase2 = global::UnityEngine.Object.Instantiate<AdminToyBase>(adminToyBase);
							adminToyBase2.OnSpawned(playerCommandSender.ReferenceHub, arguments);
							response = string.Format("Toy \"{0}\" placed! You can remove it by using \"DESTROYTOY {1}\" command.", adminToyBase2.CommandName, adminToyBase2.netId);
							return true;
						}
					}
				}
			}
			if (!flag)
			{
				response = "Toy \"" + text + "\" not found!";
			}
			return flag;
		}

		private const string ListCommand = "list";
	}
}
