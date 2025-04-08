using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CentralAuth;
using NorthwoodLib.Pools;
using PlayerRoles;
using RemoteAdmin.Interfaces;
using VoiceChat;

namespace RemoteAdmin.Communication
{
	public class RaPlayerList : IServerCommunication, IClientCommunication
	{
		public int DataId
		{
			get
			{
				return 0;
			}
		}

		public void ReceiveData(CommandSender sender, string data)
		{
			string[] array = data.Split(' ', StringSplitOptions.None);
			if (array.Length != 3)
			{
				return;
			}
			int num;
			int num2;
			if (!int.TryParse(array[0], out num) || !int.TryParse(array[1], out num2))
			{
				return;
			}
			if (!Enum.IsDefined(typeof(RaPlayerList.PlayerSorting), num2))
			{
				return;
			}
			bool flag = num == 1;
			bool flag2 = array[2].Equals("1", StringComparison.Ordinal);
			RaPlayerList.PlayerSorting playerSorting = (RaPlayerList.PlayerSorting)num2;
			bool flag3 = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
			bool flag4 = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent("\n");
			foreach (ReferenceHub referenceHub in (flag2 ? this.SortPlayersDescending(playerSorting) : this.SortPlayers(playerSorting)))
			{
				ClientInstanceMode mode = referenceHub.Mode;
				if (mode != ClientInstanceMode.DedicatedServer && mode != ClientInstanceMode.Unverified)
				{
					bool isInOverwatch = referenceHub.serverRoles.IsInOverwatch;
					bool flag5 = VoiceChatMutes.IsMuted(referenceHub, false);
					stringBuilder.Append(RaPlayerList.GetPrefix(referenceHub, flag3, flag4));
					if (isInOverwatch)
					{
						stringBuilder.Append("<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc>\uf06e</color><color=white>]</color></link> ");
					}
					if (flag5)
					{
						stringBuilder.Append("<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ");
					}
					stringBuilder.Append("<color={RA_ClassColor}>(").Append(referenceHub.PlayerId).Append(") ");
					stringBuilder.Append(referenceHub.nicknameSync.CombinedName.Replace("\n", string.Empty).Replace("RA_", string.Empty)).Append("</color>");
					stringBuilder.AppendLine();
				}
			}
			sender.RaReply(string.Format("${0} {1}", this.DataId, StringBuilderPool.Shared.ToStringReturn(stringBuilder)), true, !flag, string.Empty);
		}

		private IEnumerable<ReferenceHub> SortPlayers(RaPlayerList.PlayerSorting sortingType)
		{
			switch (sortingType)
			{
			case RaPlayerList.PlayerSorting.Alphabetical:
				return ReferenceHub.AllHubs.OrderBy((ReferenceHub h) => h.nicknameSync.DisplayName ?? h.nicknameSync.MyNick);
			case RaPlayerList.PlayerSorting.Class:
				return from h in ReferenceHub.AllHubs
					orderby h.GetTeam(), h.GetRoleId()
					select h;
			case RaPlayerList.PlayerSorting.Team:
				return ReferenceHub.AllHubs.OrderBy((ReferenceHub h) => h.roleManager.CurrentRole.Team);
			}
			return ReferenceHub.AllHubs.OrderBy((ReferenceHub h) => h.PlayerId);
		}

		private IEnumerable<ReferenceHub> SortPlayersDescending(RaPlayerList.PlayerSorting sortingType)
		{
			switch (sortingType)
			{
			case RaPlayerList.PlayerSorting.Alphabetical:
				return ReferenceHub.AllHubs.OrderByDescending((ReferenceHub h) => h.nicknameSync.DisplayName ?? h.nicknameSync.MyNick);
			case RaPlayerList.PlayerSorting.Class:
				return from h in ReferenceHub.AllHubs
					orderby h.GetTeam() descending, h.GetRoleId() descending
					select h;
			case RaPlayerList.PlayerSorting.Team:
				return ReferenceHub.AllHubs.OrderByDescending((ReferenceHub h) => h.roleManager.CurrentRole.Team);
			}
			return ReferenceHub.AllHubs.OrderByDescending((ReferenceHub h) => h.PlayerId);
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

		private const string OverwatchBadge = "<link=RA_OverwatchEnabled><color=white>[</color><color=#03f8fc>\uf06e</color><color=white>]</color></link> ";

		private const string MutedBadge = "<link=RA_Muted><color=white>[</color>\ud83d\udd07<color=white>]</color></link> ";

		public enum PlayerSorting
		{
			Ids,
			Alphabetical,
			Class,
			Team
		}
	}
}
