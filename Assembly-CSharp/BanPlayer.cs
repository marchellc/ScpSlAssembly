using System;
using CommandSystem;
using Footprinting;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using NorthwoodLib;
using RemoteAdmin;

public static class BanPlayer
{
	public static void GlobalBanUser(ReferenceHub target, ICommandSender issuer)
	{
		if (ConfigFile.ServerConfig.GetBool("gban_ban_ip", false))
		{
			BanPlayer.ApplyIpBan(target, issuer, "You have been Globally Banned.", (long)((ulong)(-1)));
		}
		ServerConsole.Disconnect(target.gameObject, "You have been Globally Banned.");
	}

	public static bool KickUser(ReferenceHub target, ICommandSender issuer, string reason)
	{
		PlayerCommandSender playerCommandSender = issuer as PlayerCommandSender;
		ReferenceHub referenceHub = ((playerCommandSender != null) ? playerCommandSender.ReferenceHub : ReferenceHub.HostHub);
		PlayerKickingEventArgs playerKickingEventArgs = new PlayerKickingEventArgs(target, referenceHub, reason);
		PlayerEvents.OnKicking(playerKickingEventArgs);
		if (!playerKickingEventArgs.IsAllowed)
		{
			return false;
		}
		reason = playerKickingEventArgs.Reason;
		ServerConsole.Disconnect(target.gameObject, "You have been kicked. Reason: " + reason);
		PlayerEvents.OnKicked(new PlayerKickedEventArgs(target, referenceHub, reason));
		return true;
	}

	public static bool KickUser(ReferenceHub target, ReferenceHub issuer, string reason)
	{
		return BanPlayer.KickUser(target, new PlayerCommandSender(issuer), reason);
	}

	public static bool KickUser(ReferenceHub target, string reason)
	{
		return BanPlayer.KickUser(target, ServerConsole.Scs, reason);
	}

	public static bool BanUser(ReferenceHub target, string reason, long duration)
	{
		return BanPlayer.BanUser(target, ServerConsole.Scs, reason, duration);
	}

	public static bool BanUser(Footprint target, string reason, long duration)
	{
		return BanPlayer.BanUser(target, ServerConsole.Scs, reason, duration);
	}

	public static bool BanUser(ReferenceHub target, ReferenceHub issuer, string reason, long duration)
	{
		return BanPlayer.BanUser(target, new PlayerCommandSender(issuer), reason, duration);
	}

	public static bool BanUser(ReferenceHub target, ICommandSender issuer, string reason, long duration)
	{
		return BanPlayer.BanUser(new Footprint(target), issuer, reason, duration);
	}

	public static bool BanUser(Footprint target, ICommandSender issuer, string reason, long duration)
	{
		if (duration == 0L && target.Hub != null)
		{
			return BanPlayer.KickUser(target.Hub, issuer, reason);
		}
		if (target.BypassStaff)
		{
			return false;
		}
		PlayerCommandSender playerCommandSender = issuer as PlayerCommandSender;
		ReferenceHub referenceHub = ((playerCommandSender != null) ? playerCommandSender.ReferenceHub : ReferenceHub.HostHub);
		PlayerBanningEventArgs playerBanningEventArgs = new PlayerBanningEventArgs(target.Hub, target.LogUserID, referenceHub, reason, duration);
		PlayerEvents.OnBanning(playerBanningEventArgs);
		if (!playerBanningEventArgs.IsAllowed)
		{
			return false;
		}
		duration = playerBanningEventArgs.Duration;
		reason = playerBanningEventArgs.Reason;
		BanPlayer.ApplyIpBan(target, issuer, reason, duration);
		long num = TimeBehaviour.CurrentTimestamp();
		long banExpirationTime = TimeBehaviour.GetBanExpirationTime((uint)duration);
		string text = BanPlayer.ValidateNick(target.Nickname);
		if (!string.IsNullOrEmpty(target.LogUserID))
		{
			BanHandler.IssueBan(new BanDetails
			{
				OriginalName = text,
				Id = target.LogUserID,
				IssuanceTime = num,
				Expires = banExpirationTime,
				Reason = reason,
				Issuer = issuer.LogName
			}, BanHandler.BanType.UserId, false);
		}
		PlayerEvents.OnBanned(new PlayerBannedEventArgs(target.Hub, target.LogUserID, referenceHub, reason, duration));
		if (target.Hub != null)
		{
			ServerConsole.Disconnect(target.Hub.gameObject, "You have been banned. Reason: " + reason);
		}
		return true;
	}

	public static void ApplyIpBan(ReferenceHub target, ICommandSender issuer, string reason, long duration)
	{
		BanPlayer.ApplyIpBan(new Footprint(target), issuer, reason, duration);
	}

	public static void ApplyIpBan(Footprint target, ICommandSender issuer, string reason, long duration)
	{
		if (!ConfigFile.ServerConfig.GetBool("ip_banning", false) || string.IsNullOrEmpty(target.IpAddress))
		{
			return;
		}
		long num = TimeBehaviour.CurrentTimestamp();
		long banExpirationTime = TimeBehaviour.GetBanExpirationTime((uint)duration);
		BanHandler.IssueBan(new BanDetails
		{
			OriginalName = BanPlayer.ValidateNick(target.Nickname),
			Id = target.IpAddress,
			IssuanceTime = num,
			Expires = banExpirationTime,
			Reason = reason,
			Issuer = issuer.LogName
		}, BanHandler.BanType.IP, false);
	}

	public static string ValidateNick(string username)
	{
		int @int = ConfigFile.ServerConfig.GetInt("ban_nickname_maxlength", 30);
		bool @bool = ConfigFile.ServerConfig.GetBool("ban_nickname_trimunicode", true);
		string text = (string.IsNullOrEmpty(username) ? "(no nick)" : username);
		if (@bool)
		{
			text = StringUtils.StripUnicodeCharacters(text, "");
		}
		if (text.Length > @int)
		{
			text = text.Substring(0, @int);
		}
		return text;
	}

	private const string _globalBanReason = "You have been Globally Banned.";
}
