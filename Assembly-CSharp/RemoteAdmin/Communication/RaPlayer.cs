using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CentralAuth;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;
using Utils;
using VoiceChat;

namespace RemoteAdmin.Communication;

public class RaPlayer : IServerCommunication, IClientCommunication
{
	public int DataId => 1;

	public void ReceiveData(CommandSender sender, string data)
	{
		string[] array = data.Split(' ');
		if (array.Length != 2 || !int.TryParse(array[0], out var result))
		{
			return;
		}
		bool flag = result == 1;
		PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
		if (!flag && playerCommandSender != null && !playerCommandSender.ReferenceHub.authManager.RemoteAdminGlobalAccess && !playerCommandSender.ReferenceHub.authManager.BypassBansFlagSet && !CommandProcessor.CheckPermissions(sender, PlayerPermissions.PlayerSensitiveDataAccess))
		{
			return;
		}
		string[] newargs;
		List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(new ArraySegment<string>(array.Skip(1).ToArray()), 0, out newargs);
		if (list.Count == 0)
		{
			return;
		}
		bool flag2 = (playerCommandSender != null && playerCommandSender.ReferenceHub.authManager.NorthwoodStaff) || PermissionsHandler.IsPermitted(sender.Permissions, 18007046uL);
		if (list.Count > 1)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.AppendFormat("${0} ", DataId);
			stringBuilder.Append("<color=white>Multiple players selected:");
			stringBuilder.Append("\nPlayer ID: <color=green><link=CP_ID>\uf0c5</link></color>");
			stringBuilder.AppendFormat("\nIP Address: {0}", (!flag) ? "<color=green><link=CP_IP>\uf0c5</link></color>" : "[REDACTED]");
			stringBuilder.AppendFormat("\nUser ID: {0}", flag2 ? "<color=green><link=CP_USERID>\uf0c5</link></color>" : "[REDACTED]");
			stringBuilder.Append("</color>");
			StringBuilder stringBuilder2 = StringBuilderPool.Shared.Rent();
			StringBuilder stringBuilder3 = ((!flag) ? StringBuilderPool.Shared.Rent() : null);
			StringBuilder stringBuilder4 = (flag2 ? StringBuilderPool.Shared.Rent() : null);
			foreach (ReferenceHub item in list)
			{
				stringBuilder2.Append(item.PlayerId);
				stringBuilder2.Append(", ");
				if (!flag)
				{
					ServerLogs.AddLog(ServerLogs.Modules.DataAccess, $"{sender.LogName} accessed IP address of player {item.PlayerId} ({item.nicknameSync.MyNick}).", ServerLogs.ServerLogType.RemoteAdminActivity_Misc);
					stringBuilder3.Append(item.networkIdentity.connectionToClient.address);
					stringBuilder3.Append(", ");
				}
				if (flag2)
				{
					stringBuilder4.Append(item.authManager.UserId);
					stringBuilder4.Append(", ");
				}
			}
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.PlayerId, StringBuilderPool.Shared.ToStringReturn(stringBuilder2));
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.Ip, (stringBuilder3 == null) ? string.Empty : StringBuilderPool.Shared.ToStringReturn(stringBuilder3));
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.UserId, (stringBuilder4 == null) ? string.Empty : StringBuilderPool.Shared.ToStringReturn(stringBuilder4));
			sender.RaReply(StringBuilderPool.Shared.ToStringReturn(stringBuilder), success: true, logToConsole: true, string.Empty);
			return;
		}
		ReferenceHub referenceHub = list[0];
		bool flag3 = PermissionsHandler.IsPermitted(sender.Permissions, PlayerPermissions.GameplayData);
		CharacterClassManager characterClassManager = referenceHub.characterClassManager;
		PlayerAuthenticationManager authManager = referenceHub.authManager;
		NicknameSync nicknameSync = referenceHub.nicknameSync;
		NetworkConnectionToClient connectionToClient = referenceHub.networkIdentity.connectionToClient;
		ServerRoles serverRoles = referenceHub.serverRoles;
		if (sender is PlayerCommandSender playerCommandSender2)
		{
			playerCommandSender2.ReferenceHub.queryProcessor.GameplayData = flag3;
		}
		StringBuilder stringBuilder5 = StringBuilderPool.Shared.Rent();
		stringBuilder5.AppendFormat("${0} ", DataId);
		stringBuilder5.AppendFormat("<color=white>Nickname: {0}", nicknameSync.CombinedName);
		stringBuilder5.AppendFormat("\nPlayer ID: {0} <color=green><link=CP_ID>\uf0c5</link></color>", referenceHub.PlayerId);
		RaClipboard.Send(sender, RaClipboard.RaClipBoardType.PlayerId, $"{referenceHub.PlayerId}");
		if (connectionToClient == null)
		{
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.Ip, string.Empty);
			stringBuilder5.Append("\nIP Address: null");
		}
		else if (!flag)
		{
			ServerLogs.AddLog(ServerLogs.Modules.DataAccess, $"{sender.LogName} accessed IP address of player {referenceHub.PlayerId} ({referenceHub.nicknameSync.MyNick}).", ServerLogs.ServerLogType.RemoteAdminActivity_Misc);
			string address = connectionToClient.address;
			stringBuilder5.AppendFormat("\nIP Address: {0} ", address);
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.Ip, address);
			if (connectionToClient.IpOverride != null)
			{
				stringBuilder5.AppendFormat(" [routed via {0}]", connectionToClient.OriginalIpAddress);
			}
			stringBuilder5.Append(" <color=green><link=CP_IP>\uf0c5</link></color>");
		}
		else
		{
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.Ip, string.Empty);
			stringBuilder5.Append("\nIP Address: [REDACTED]");
		}
		stringBuilder5.Append("\nUser ID: ");
		if (flag2)
		{
			if (string.IsNullOrEmpty(authManager.UserId))
			{
				stringBuilder5.Append("(none)");
			}
			else
			{
				stringBuilder5.AppendFormat("{0} <color=green><link=CP_USERID>\uf0c5</link></color>", authManager.UserId);
			}
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.UserId, authManager.UserId ?? string.Empty);
			if (authManager.SaltedUserId != null && authManager.SaltedUserId.Contains("$", StringComparison.Ordinal))
			{
				stringBuilder5.AppendFormat("\nSalted User ID: {0}", authManager.SaltedUserId);
			}
		}
		else
		{
			stringBuilder5.Append("<color=#D4AF37>INSUFFICIENT PERMISSIONS</color>");
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.UserId, string.Empty);
		}
		stringBuilder5.Append("\nServer role: ");
		stringBuilder5.Append(serverRoles.GetColoredRoleString());
		bool flag4 = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenBadges);
		bool flag5 = CommandProcessor.CheckPermissions(sender, PlayerPermissions.ViewHiddenGlobalBadges);
		if (playerCommandSender != null)
		{
			flag4 = true;
			flag5 = true;
		}
		bool flag6 = !string.IsNullOrEmpty(serverRoles.HiddenBadge);
		bool flag7 = !flag6 || (serverRoles.GlobalHidden && flag5) || (!serverRoles.GlobalHidden && flag4);
		if (flag7)
		{
			if (flag6)
			{
				stringBuilder5.AppendFormat("\n<color=#DC143C>Hidden role: </color>{0}", serverRoles.HiddenBadge);
				stringBuilder5.AppendFormat("\n<color=#DC143C>Hidden role type: </color>{0}", serverRoles.GlobalHidden ? "GLOBAL" : "LOCAL");
			}
			if (referenceHub.authManager.RemoteAdminGlobalAccess)
			{
				stringBuilder5.Append("\nStudio Status: <color=#BCC6CC>Studio GLOBAL Staff (management or global moderation)</color>");
			}
			else if (referenceHub.authManager.NorthwoodStaff)
			{
				stringBuilder5.Append("\nStudio Status: <color=#94B9CF>Studio Staff</color>");
			}
		}
		VcMuteFlags flags = VoiceChatMutes.GetFlags(list[0]);
		if (flags != 0)
		{
			stringBuilder5.Append("\nMUTE STATUS:");
			VcMuteFlags[] values = EnumUtils<VcMuteFlags>.Values;
			foreach (VcMuteFlags vcMuteFlags in values)
			{
				if (vcMuteFlags != 0 && (flags & vcMuteFlags) == vcMuteFlags)
				{
					stringBuilder5.Append(" <color=#F70D1A>");
					stringBuilder5.Append(vcMuteFlags);
					stringBuilder5.Append("</color>");
				}
			}
		}
		stringBuilder5.Append("\nActive flag(s):");
		if (characterClassManager.GodMode)
		{
			stringBuilder5.Append(" <color=#659EC7>[GOD MODE]</color>");
		}
		if (referenceHub.playerStats.GetModule<AdminFlagsStat>().HasFlag(AdminFlags.Noclip))
		{
			stringBuilder5.Append(" <color=#DC143C>[NOCLIP ENABLED]</color>");
		}
		else if (FpcNoclip.IsPermitted(referenceHub))
		{
			stringBuilder5.Append(" <color=#E52B50>[NOCLIP UNLOCKED]</color>");
		}
		if (authManager.DoNotTrack)
		{
			stringBuilder5.Append(" <color=#BFFF00>[DO NOT TRACK]</color>");
		}
		if (serverRoles.BypassMode)
		{
			stringBuilder5.Append(" <color=#BFFF00>[BYPASS MODE]</color>");
		}
		if (flag7 && serverRoles.RemoteAdmin)
		{
			stringBuilder5.Append(" <color=#43C6DB>[RA AUTHENTICATED]</color>");
		}
		if (serverRoles.IsInOverwatch)
		{
			stringBuilder5.Append(" <color=#008080>[OVERWATCH MODE]</color>");
		}
		else if (flag3)
		{
			stringBuilder5.Append("\nClass: ").Append(PlayerRoleLoader.AllRoles.TryGetValue(referenceHub.GetRoleId(), out var value) ? ((object)value.RoleTypeId) : "None");
			stringBuilder5.Append(" <color=#fcff99>[HP: ").Append(CommandProcessor.GetRoundedStat<HealthStat>(referenceHub)).Append("]</color>");
			stringBuilder5.Append(" <color=green>[AHP: ").Append(CommandProcessor.GetRoundedStat<AhpStat>(referenceHub)).Append("]</color>");
			stringBuilder5.Append(" <color=#977dff>[HS: ").Append(CommandProcessor.GetRoundedStat<HumeShieldStat>(referenceHub)).Append("]</color>");
			stringBuilder5.Append("\nPosition: ").Append(referenceHub.transform.position.ToPreciseString());
		}
		else
		{
			stringBuilder5.Append("\n<color=#D4AF37>Some fields were hidden. GameplayData permission required.</color>");
		}
		stringBuilder5.Append("</color>");
		sender.RaReply(StringBuilderPool.Shared.ToStringReturn(stringBuilder5), success: true, logToConsole: true, string.Empty);
		RaPlayerQR.Send(sender, isBig: false, (!flag2 || string.IsNullOrEmpty(authManager.UserId)) ? string.Empty : authManager.UserId);
	}

	public void ReceiveData(string data, bool secure)
	{
	}
}
