using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CentralAuth;
using NorthwoodLib.Pools;
using PlayerRoles;
using RemoteAdmin.Interfaces;
using VoiceChat;

namespace RemoteAdmin.Communication;

public class RaPlayerList : IServerCommunication, IClientCommunication
{
	public enum PlayerSorting
	{
		Ids,
		Alphabetical,
		Class,
		Team
	}

	private const string OverwatchBadge = "<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc>\uf06e</color><color=white>]</color></link> ";

	private const string MutedBadge = "<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ";

	public int DataId => 0;

	public void ReceiveData(CommandSender sender, string data)
	{
		string[] array = data.Split(' ');
		if (array.Length != 3 || !int.TryParse(array[0], out var result) || !int.TryParse(array[1], out var result2) || !Enum.IsDefined(typeof(PlayerSorting), result2))
		{
			return;
		}
		bool flag = result == 1;
		bool num = array[2].Equals("1", StringComparison.Ordinal);
		PlayerSorting sortingType = (PlayerSorting)result2;
		bool viewHiddenBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
		bool viewHiddenGlobalBadges = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent("\n");
		foreach (ReferenceHub item in num ? this.SortPlayersDescending(sortingType) : this.SortPlayers(sortingType))
		{
			ClientInstanceMode mode = item.Mode;
			if (mode != ClientInstanceMode.DedicatedServer && mode != ClientInstanceMode.Unverified)
			{
				bool isInOverwatch = item.serverRoles.IsInOverwatch;
				bool flag2 = VoiceChatMutes.IsMuted(item);
				stringBuilder.Append(RaPlayerList.GetPrefix(item, viewHiddenBadges, viewHiddenGlobalBadges));
				if (isInOverwatch)
				{
					stringBuilder.Append("<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc>\uf06e</color><color=white>]</color></link> ");
				}
				if (flag2)
				{
					stringBuilder.Append("<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ");
				}
				stringBuilder.Append("<color={RA_ClassColor}>(").Append(item.PlayerId).Append(") ");
				stringBuilder.Append(item.nicknameSync.CombinedName.Replace("\n", string.Empty).Replace("RA_", string.Empty)).Append("</color>");
				stringBuilder.AppendLine();
			}
		}
		sender.RaReply($"${this.DataId} {StringBuilderPool.Shared.ToStringReturn(stringBuilder)}", success: true, !flag, string.Empty);
	}

	private IEnumerable<ReferenceHub> SortPlayers(PlayerSorting sortingType)
	{
		return sortingType switch
		{
			PlayerSorting.Team => ReferenceHub.AllHubs.OrderBy((ReferenceHub h) => h.roleManager.CurrentRole.Team), 
			PlayerSorting.Alphabetical => ReferenceHub.AllHubs.OrderBy((ReferenceHub h) => h.nicknameSync.DisplayName ?? h.nicknameSync.MyNick), 
			PlayerSorting.Class => from h in ReferenceHub.AllHubs
				orderby h.GetTeam(), h.GetRoleId()
				select h, 
			_ => ReferenceHub.AllHubs.OrderBy((ReferenceHub h) => h.PlayerId), 
		};
	}

	private IEnumerable<ReferenceHub> SortPlayersDescending(PlayerSorting sortingType)
	{
		return sortingType switch
		{
			PlayerSorting.Team => ReferenceHub.AllHubs.OrderByDescending((ReferenceHub h) => h.roleManager.CurrentRole.Team), 
			PlayerSorting.Alphabetical => ReferenceHub.AllHubs.OrderByDescending((ReferenceHub h) => h.nicknameSync.DisplayName ?? h.nicknameSync.MyNick), 
			PlayerSorting.Class => from h in ReferenceHub.AllHubs
				orderby h.GetTeam() descending, h.GetRoleId() descending
				select h, 
			_ => ReferenceHub.AllHubs.OrderByDescending((ReferenceHub h) => h.PlayerId), 
		};
	}

	private static string GetPrefix(ReferenceHub hub, bool viewHiddenBadges = false, bool viewHiddenGlobalBadges = false)
	{
		if (hub.IsDummy)
		{
			return "[<color=#fcba03>\ud83d\udcbb</color>] ";
		}
		ServerRoles serverRoles = hub.serverRoles;
		if (!string.IsNullOrEmpty(serverRoles.HiddenBadge) && (!serverRoles.GlobalHidden || !viewHiddenBadges) && (serverRoles.GlobalHidden || !viewHiddenGlobalBadges))
		{
			return string.Empty;
		}
		if (hub.authManager.RemoteAdminGlobalAccess)
		{
			return "<link=RA_RaEverywhere><color=white>[<color=#EFC01A>\uf3ed</color><color=white>]</color></link> ";
		}
		if (hub.authManager.NorthwoodStaff)
		{
			return "<link=RA_StudioStaff><color=white>[<color=#005EBC>\uf0ad</color><color=white>]</color></link> ";
		}
		if (serverRoles.RemoteAdmin)
		{
			return "<link=RA_Admin><color=white>[\uf406]</color></link> ";
		}
		return string.Empty;
	}

	public void ReceiveData(string data, bool secure)
	{
	}
}
