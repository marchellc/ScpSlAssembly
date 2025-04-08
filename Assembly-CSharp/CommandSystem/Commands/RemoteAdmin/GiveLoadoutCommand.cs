using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using InventorySystem;
using InventorySystem.Configs;
using PlayerRoles;
using Utils;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class GiveLoadoutCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "giveloadout";

		public string[] Aliases { get; } = new string[] { "sendloadout", "giveinventory", "grantloadout" };

		public string Description { get; } = "Grant target(s) the specified role's loadout.";

		public string[] Usage { get; } = new string[] { "%player%", "%role%" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHostHub(out referenceHub))
			{
				response = "You are not connected to a server.";
				return false;
			}
			if (arguments.Count < 2)
			{
				response = "To execute this command provide at least 2 arguments!\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			if (!sender.CheckPermission(PlayerPermissions.GivingItems, out response))
			{
				return false;
			}
			CharacterClassManager characterClassManager = referenceHub.characterClassManager;
			if (characterClassManager == null || !characterClassManager.isLocalPlayer || !characterClassManager.isServer || !characterClassManager.RoundStarted)
			{
				response = "Please start round before using this command.";
				return false;
			}
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			PlayerRoleBase playerRoleBase;
			if (!this.TryParseRole(array[0], out playerRoleBase))
			{
				response = "Invalid role ID / name.";
				return false;
			}
			if (!StartingInventories.DefinedInventories.ContainsKey(playerRoleBase.RoleTypeId))
			{
				response = "Specified role does not have a defined inventory.";
				return false;
			}
			RoleSpawnFlags roleSpawnFlags;
			this.ProvideRoleFlag(array, out roleSpawnFlags);
			bool flag = roleSpawnFlags.HasFlag(RoleSpawnFlags.AssignInventory);
			int num = 0;
			foreach (ReferenceHub referenceHub2 in list)
			{
				if (!(referenceHub2 == null))
				{
					InventoryItemProvider.ServerGrantLoadout(referenceHub2, playerRoleBase, flag);
					ServerLogs.AddLog(ServerLogs.Modules.ClassChange, string.Format("{0} gave {1} the loadout of {2}.", sender.LogName, referenceHub2.LoggedNameFromRefHub(), playerRoleBase.RoleTypeId), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					num++;
				}
			}
			response = string.Format("Done! Given {0}'s loadout to {1} player{2}!", playerRoleBase.RoleName, num, (num == 1) ? "" : "s");
			return true;
		}

		private void ProvideRoleFlag(string[] arguments, out RoleSpawnFlags spawnFlags)
		{
			byte b;
			if (arguments.Length > 1 && byte.TryParse(arguments[1], out b))
			{
				spawnFlags = (RoleSpawnFlags)b;
				return;
			}
			spawnFlags = RoleSpawnFlags.All;
		}

		private bool TryParseRole(string s, out PlayerRoleBase prb)
		{
			RoleTypeId roleTypeId;
			if (Enum.TryParse<RoleTypeId>(s, true, out roleTypeId))
			{
				return PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(roleTypeId, out prb);
			}
			foreach (PlayerRoleBase playerRoleBase in PlayerRoleLoader.AllRoles.Values)
			{
				if (!string.Equals(Regex.Replace(playerRoleBase.RoleName, "\\s+", ""), s, StringComparison.InvariantCultureIgnoreCase))
				{
					prb = playerRoleBase;
					return true;
				}
			}
			prb = null;
			return false;
		}
	}
}
