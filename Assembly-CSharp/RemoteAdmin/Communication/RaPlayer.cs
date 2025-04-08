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

namespace RemoteAdmin.Communication
{
	public class RaPlayer : IServerCommunication, IClientCommunication
	{
		public int DataId
		{
			get
			{
				return 1;
			}
		}

		public void ReceiveData(CommandSender sender, string data)
		{
			string[] array = data.Split(' ', StringSplitOptions.None);
			if (array.Length != 2)
			{
				return;
			}
			int num;
			if (!int.TryParse(array[0], out num))
			{
				return;
			}
			bool flag = num == 1;
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (!flag && playerCommandSender != null && !playerCommandSender.ReferenceHub.authManager.RemoteAdminGlobalAccess && !playerCommandSender.ReferenceHub.authManager.BypassBansFlagSet && !CommandProcessor.CheckPermissions(sender, PlayerPermissions.PlayerSensitiveDataAccess))
			{
				return;
			}
			string[] array2;
			List<ReferenceHub> list = RAUtils.ProcessPlayerIdOrNamesList(new ArraySegment<string>(array.Skip(1).ToArray<string>()), 0, out array2, false);
			if (list.Count == 0)
			{
				return;
			}
			bool flag2 = (playerCommandSender != null && playerCommandSender.ReferenceHub.authManager.NorthwoodStaff) || PermissionsHandler.IsPermitted(sender.Permissions, 18007046UL);
			if (list.Count > 1)
			{
				StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
				stringBuilder.AppendFormat("${0} ", this.DataId);
				stringBuilder.Append("<color=white>Multiple players selected:");
				stringBuilder.Append("\nPlayer ID: <color=green><link=CP_ID>\uf0c5</link></color>");
				stringBuilder.AppendFormat("\nIP Address: {0}", (!flag) ? "<color=green><link=CP_IP>\uf0c5</link></color>" : "[REDACTED]");
				stringBuilder.AppendFormat("\nUser ID: {0}", flag2 ? "<color=green><link=CP_USERID>\uf0c5</link></color>" : "[REDACTED]");
				stringBuilder.Append("</color>");
				StringBuilder stringBuilder2 = StringBuilderPool.Shared.Rent();
				StringBuilder stringBuilder3 = ((!flag) ? StringBuilderPool.Shared.Rent() : null);
				StringBuilder stringBuilder4 = (flag2 ? StringBuilderPool.Shared.Rent() : null);
				foreach (ReferenceHub referenceHub in list)
				{
					stringBuilder2.Append(referenceHub.PlayerId);
					stringBuilder2.Append(", ");
					if (!flag)
					{
						ServerLogs.AddLog(ServerLogs.Modules.DataAccess, string.Format("{0} accessed IP address of player {1} ({2}).", sender.LogName, referenceHub.PlayerId, referenceHub.nicknameSync.MyNick), ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
						stringBuilder3.Append(referenceHub.networkIdentity.connectionToClient.address);
						stringBuilder3.Append(", ");
					}
					if (flag2)
					{
						stringBuilder4.Append(referenceHub.authManager.UserId);
						stringBuilder4.Append(", ");
					}
				}
				RaClipboard.Send(sender, RaClipboard.RaClipBoardType.PlayerId, StringBuilderPool.Shared.ToStringReturn(stringBuilder2));
				RaClipboard.Send(sender, RaClipboard.RaClipBoardType.Ip, (stringBuilder3 == null) ? string.Empty : StringBuilderPool.Shared.ToStringReturn(stringBuilder3));
				RaClipboard.Send(sender, RaClipboard.RaClipBoardType.UserId, (stringBuilder4 == null) ? string.Empty : StringBuilderPool.Shared.ToStringReturn(stringBuilder4));
				sender.RaReply(StringBuilderPool.Shared.ToStringReturn(stringBuilder), true, true, string.Empty);
				return;
			}
			ReferenceHub referenceHub2 = list[0];
			bool flag3 = PermissionsHandler.IsPermitted(sender.Permissions, PlayerPermissions.GameplayData);
			CharacterClassManager characterClassManager = referenceHub2.characterClassManager;
			PlayerAuthenticationManager authManager = referenceHub2.authManager;
			NicknameSync nicknameSync = referenceHub2.nicknameSync;
			NetworkConnectionToClient connectionToClient = referenceHub2.networkIdentity.connectionToClient;
			ServerRoles serverRoles = referenceHub2.serverRoles;
			PlayerCommandSender playerCommandSender2 = sender as PlayerCommandSender;
			if (playerCommandSender2 != null)
			{
				playerCommandSender2.ReferenceHub.queryProcessor.GameplayData = flag3;
			}
			StringBuilder stringBuilder5 = StringBuilderPool.Shared.Rent();
			stringBuilder5.AppendFormat("${0} ", this.DataId);
			stringBuilder5.AppendFormat("<color=white>Nickname: {0}", nicknameSync.CombinedName);
			stringBuilder5.AppendFormat("\nPlayer ID: {0} <color=green><link=CP_ID>\uf0c5</link></color>", referenceHub2.PlayerId);
			RaClipboard.Send(sender, RaClipboard.RaClipBoardType.PlayerId, string.Format("{0}", referenceHub2.PlayerId));
			if (connectionToClient == null)
			{
				RaClipboard.Send(sender, RaClipboard.RaClipBoardType.Ip, string.Empty);
				stringBuilder5.Append("\nIP Address: null");
			}
			else if (!flag)
			{
				ServerLogs.AddLog(ServerLogs.Modules.DataAccess, string.Format("{0} accessed IP address of player {1} ({2}).", sender.LogName, referenceHub2.PlayerId, referenceHub2.nicknameSync.MyNick), ServerLogs.ServerLogType.RemoteAdminActivity_Misc, false);
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
			stringBuilder5.Append(serverRoles.GetColoredRoleString(false));
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
				if (referenceHub2.authManager.RemoteAdminGlobalAccess)
				{
					stringBuilder5.Append("\nStudio Status: <color=#BCC6CC>Studio GLOBAL Staff (management or global moderation)</color>");
				}
				else if (referenceHub2.authManager.NorthwoodStaff)
				{
					stringBuilder5.Append("\nStudio Status: <color=#94B9CF>Studio Staff</color>");
				}
			}
			VcMuteFlags flags = VoiceChatMutes.GetFlags(list[0]);
			if (flags != VcMuteFlags.None)
			{
				stringBuilder5.Append("\nMUTE STATUS:");
				foreach (VcMuteFlags vcMuteFlags in EnumUtils<VcMuteFlags>.Values)
				{
					if (vcMuteFlags != VcMuteFlags.None && (flags & vcMuteFlags) == vcMuteFlags)
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
			if (referenceHub2.playerStats.GetModule<AdminFlagsStat>().HasFlag(AdminFlags.Noclip))
			{
				stringBuilder5.Append(" <color=#DC143C>[NOCLIP ENABLED]</color>");
			}
			else if (FpcNoclip.IsPermitted(referenceHub2))
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
				PlayerRoleBase playerRoleBase;
				stringBuilder5.Append("\nClass: ").Append(PlayerRoleLoader.AllRoles.TryGetValue(referenceHub2.GetRoleId(), out playerRoleBase) ? playerRoleBase.RoleTypeId : "None");
				stringBuilder5.Append(" <color=#fcff99>[HP: ").Append(CommandProcessor.GetRoundedStat<HealthStat>(referenceHub2)).Append("]</color>");
				stringBuilder5.Append(" <color=green>[AHP: ").Append(CommandProcessor.GetRoundedStat<AhpStat>(referenceHub2)).Append("]</color>");
				stringBuilder5.Append(" <color=#977dff>[HS: ").Append(CommandProcessor.GetRoundedStat<HumeShieldStat>(referenceHub2)).Append("]</color>");
				stringBuilder5.Append("\nPosition: ").Append(referenceHub2.transform.position.ToPreciseString());
			}
			else
			{
				stringBuilder5.Append("\n<color=#D4AF37>Some fields were hidden. GameplayData permission required.</color>");
			}
			stringBuilder5.Append("</color>");
			sender.RaReply(StringBuilderPool.Shared.ToStringReturn(stringBuilder5), true, true, string.Empty);
			RaPlayerQR.Send(sender, false, (!flag2 || string.IsNullOrEmpty(authManager.UserId)) ? string.Empty : authManager.UserId);
		}

		public void ReceiveData(string data, bool secure)
		{
		}
	}
}
