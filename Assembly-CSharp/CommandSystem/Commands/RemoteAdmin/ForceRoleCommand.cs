using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using PlayerRoles;
using RemoteAdmin;
using Utils;
using Utils.NonAllocLINQ;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class ForceRoleCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "forcerole";

		public string[] Aliases { get; } = new string[] { "fc", "fr", "forceclass" };

		public string Description { get; } = "Forces a player to a specified role.";

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
			string[] array;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(arguments, 0, out array, false);
			bool flag;
			if (list.Count == 1)
			{
				PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
				if (playerCommandSender != null)
				{
					flag = playerCommandSender.ReferenceHub == list[0];
					goto IL_0071;
				}
			}
			flag = false;
			IL_0071:
			bool flag2 = flag;
			PlayerRoleBase playerRoleBase;
			if (!this.TryParseRole(array[0], out playerRoleBase))
			{
				response = "Invalid role ID / name.";
				return false;
			}
			if (!this.HasPerms(playerRoleBase.RoleTypeId, flag2, sender, out response))
			{
				return false;
			}
			RoleSpawnFlags roleSpawnFlags;
			this.ProvideRoleFlag(array, out roleSpawnFlags);
			bool flag3 = list.Any((ReferenceHub p) => p.GetRoleId() != RoleTypeId.Overwatch);
			int num = 0;
			foreach (ReferenceHub referenceHub2 in list)
			{
				if (!(referenceHub2 == null) && (!flag3 || referenceHub2.GetRoleId() != RoleTypeId.Overwatch))
				{
					referenceHub2.roleManager.ServerSetRole(playerRoleBase.RoleTypeId, RoleChangeReason.RemoteAdmin, roleSpawnFlags);
					ServerLogs.AddLog(ServerLogs.Modules.ClassChange, string.Format("{0} changed role of player {1} to {2}.", sender.LogName, referenceHub2.LoggedNameFromRefHub(), playerRoleBase.RoleTypeId), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
					num++;
				}
			}
			response = string.Format("Done! Changed role of {0} player{1} to {2}!", num, (num == 1) ? "" : "s", playerRoleBase.RoleTypeId);
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

		private bool HasPerms(RoleTypeId targetRole, bool self, ICommandSender sender, out string response)
		{
			if (targetRole != RoleTypeId.Spectator)
			{
				if (targetRole != RoleTypeId.Overwatch)
				{
					if (self)
					{
						return sender.CheckPermission(new PlayerPermissions[]
						{
							PlayerPermissions.ForceclassWithoutRestrictions,
							PlayerPermissions.ForceclassSelf
						}, out response);
					}
					return sender.CheckPermission(PlayerPermissions.ForceclassWithoutRestrictions, out response);
				}
				else
				{
					if (self)
					{
						return sender.CheckPermission(PlayerPermissions.Overwatch, out response);
					}
					return sender.CheckPermission(PlayerPermissions.Overwatch, out response) && sender.CheckPermission(new PlayerPermissions[]
					{
						PlayerPermissions.ForceclassWithoutRestrictions,
						PlayerPermissions.ForceclassToSpectator
					}, out response);
				}
			}
			else
			{
				if (self)
				{
					return sender.CheckPermission(new PlayerPermissions[]
					{
						PlayerPermissions.ForceclassWithoutRestrictions,
						PlayerPermissions.ForceclassToSpectator,
						PlayerPermissions.ForceclassSelf,
						PlayerPermissions.Overwatch
					}, out response);
				}
				return sender.CheckPermission(new PlayerPermissions[]
				{
					PlayerPermissions.ForceclassWithoutRestrictions,
					PlayerPermissions.ForceclassToSpectator
				}, out response);
			}
		}
	}
}
