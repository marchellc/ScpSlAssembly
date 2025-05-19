using CommandSystem;
using Footprinting;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using NorthwoodLib;
using RemoteAdmin;

public static class BanPlayer
{
	private const string _globalBanReason = "You have been Globally Banned.";

	public static void GlobalBanUser(ReferenceHub target, ICommandSender issuer)
	{
		if (ConfigFile.ServerConfig.GetBool("gban_ban_ip"))
		{
			ApplyIpBan(target, issuer, "You have been Globally Banned.", 4294967295L);
		}
		ServerConsole.Disconnect(target.gameObject, "You have been Globally Banned.");
	}

	public static bool KickUser(ReferenceHub target, ICommandSender issuer, string reason)
	{
		ReferenceHub issuer2 = ((issuer is PlayerCommandSender playerCommandSender) ? playerCommandSender.ReferenceHub : ReferenceHub.HostHub);
		PlayerKickingEventArgs playerKickingEventArgs = new PlayerKickingEventArgs(target, issuer2, reason);
		PlayerEvents.OnKicking(playerKickingEventArgs);
		if (!playerKickingEventArgs.IsAllowed)
		{
			return false;
		}
		reason = playerKickingEventArgs.Reason;
		ServerConsole.Disconnect(target.gameObject, "You have been kicked. Reason: " + reason);
		PlayerEvents.OnKicked(new PlayerKickedEventArgs(target, issuer2, reason));
		return true;
	}

	public static bool KickUser(ReferenceHub target, ReferenceHub issuer, string reason)
	{
		return KickUser(target, new PlayerCommandSender(issuer), reason);
	}

	public static bool KickUser(ReferenceHub target, string reason)
	{
		return KickUser(target, ServerConsole.Scs, reason);
	}

	public static bool BanUser(ReferenceHub target, string reason, long duration)
	{
		return BanUser(target, ServerConsole.Scs, reason, duration);
	}

	public static bool BanUser(Footprint target, string reason, long duration)
	{
		return BanUser(target, ServerConsole.Scs, reason, duration);
	}

	public static bool BanUser(ReferenceHub target, ReferenceHub issuer, string reason, long duration)
	{
		return BanUser(target, new PlayerCommandSender(issuer), reason, duration);
	}

	public static bool BanUser(ReferenceHub target, ICommandSender issuer, string reason, long duration)
	{
		return BanUser(new Footprint(target), issuer, reason, duration);
	}

	public static bool BanUser(Footprint target, ICommandSender issuer, string reason, long duration)
	{
		if (duration == 0L && target.Hub != null)
		{
			return KickUser(target.Hub, issuer, reason);
		}
		if (target.BypassStaff)
		{
			return false;
		}
		ReferenceHub issuer2 = ((issuer is PlayerCommandSender playerCommandSender) ? playerCommandSender.ReferenceHub : ReferenceHub.HostHub);
		PlayerBanningEventArgs playerBanningEventArgs = new PlayerBanningEventArgs(target.Hub, target.LogUserID, issuer2, reason, duration);
		PlayerEvents.OnBanning(playerBanningEventArgs);
		if (!playerBanningEventArgs.IsAllowed)
		{
			return false;
		}
		duration = playerBanningEventArgs.Duration;
		reason = playerBanningEventArgs.Reason;
		ApplyIpBan(target, issuer, reason, duration);
		long issuanceTime = TimeBehaviour.CurrentTimestamp();
		long banExpirationTime = TimeBehaviour.GetBanExpirationTime((uint)duration);
		string originalName = ValidateNick(target.Nickname);
		if (!string.IsNullOrEmpty(target.LogUserID))
		{
			BanHandler.IssueBan(new BanDetails
			{
				OriginalName = originalName,
				Id = target.LogUserID,
				IssuanceTime = issuanceTime,
				Expires = banExpirationTime,
				Reason = reason,
				Issuer = issuer.LogName
			}, BanHandler.BanType.UserId);
		}
		PlayerEvents.OnBanned(new PlayerBannedEventArgs(target.Hub, target.LogUserID, issuer2, reason, duration));
		if (target.Hub != null)
		{
			ServerConsole.Disconnect(target.Hub.gameObject, "You have been banned. Reason: " + reason);
		}
		return true;
	}

	public static void ApplyIpBan(ReferenceHub target, ICommandSender issuer, string reason, long duration)
	{
		ApplyIpBan(new Footprint(target), issuer, reason, duration);
	}

	public static void ApplyIpBan(Footprint target, ICommandSender issuer, string reason, long duration)
	{
		if (ConfigFile.ServerConfig.GetBool("ip_banning") && !string.IsNullOrEmpty(target.IpAddress))
		{
			long issuanceTime = TimeBehaviour.CurrentTimestamp();
			long banExpirationTime = TimeBehaviour.GetBanExpirationTime((uint)duration);
			BanHandler.IssueBan(new BanDetails
			{
				OriginalName = ValidateNick(target.Nickname),
				Id = target.IpAddress,
				IssuanceTime = issuanceTime,
				Expires = banExpirationTime,
				Reason = reason,
				Issuer = issuer.LogName
			}, BanHandler.BanType.IP);
		}
	}

	public static string ValidateNick(string username)
	{
		int @int = ConfigFile.ServerConfig.GetInt("ban_nickname_maxlength", 30);
		bool @bool = ConfigFile.ServerConfig.GetBool("ban_nickname_trimunicode", def: true);
		string text = (string.IsNullOrEmpty(username) ? "(no nick)" : username);
		if (@bool)
		{
			text = StringUtils.StripUnicodeCharacters(text);
		}
		if (text.Length > @int)
		{
			text = text.Substring(0, @int);
		}
		return text;
	}
}
