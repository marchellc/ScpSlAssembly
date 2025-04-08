using System;
using System.Threading;
using CentralAuth;
using Footprinting;
using PlayerStatsSystem;

internal class FriendlyFireDetector
{
	internal uint Kills { get; private set; }

	internal float Damage { get; private set; }

	internal FriendlyFireDetector(ReferenceHub hub)
	{
		this._hub = hub;
	}

	public virtual bool RegisterDamage(float damage)
	{
		this.Damage += damage;
		return false;
	}

	public virtual bool RegisterKill()
	{
		uint kills = this.Kills;
		this.Kills = kills + 1U;
		return false;
	}

	public virtual void Reset()
	{
		this.Kills = 0U;
		this.Damage = 0f;
	}

	protected void TakeAction(ref FriendlyFireAction action, string detector, ref long banDuration, ref string banReason, ref string killReason, ref string adminchat, ref string broadcast, ref bool webhook)
	{
		this._triggered = true;
		FriendlyFireDetector.TakeAction(new Footprint(this._hub), ref action, detector, ref banDuration, ref banReason, ref killReason, ref adminchat, ref broadcast, ref webhook);
	}

	internal static void TakeAction(Footprint fp, ref FriendlyFireAction action, string detector, ref long banDuration, ref string banReason, ref string killReason, ref string adminchat, ref string broadcast, ref bool webhook)
	{
		string text = ((fp.Hub != null) ? fp.Hub.roleManager.CurrentRole.RoleTypeId.ToString() : fp.Role.ToString());
		if (!string.IsNullOrWhiteSpace(adminchat) && FriendlyFireConfig.AdminChatTime > 0)
		{
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.serverRoles.AdminChatPerms && referenceHub.Mode == ClientInstanceMode.ReadyClient)
				{
					referenceHub.queryProcessor.SendToClient(string.Format("@!{0} {1}", FriendlyFireConfig.AdminChatTime, adminchat.Replace("%nick", fp.Nickname)), true, false, string.Empty);
				}
			}
		}
		if (!string.IsNullOrWhiteSpace(broadcast) && FriendlyFireConfig.BroadcastTime > 0)
		{
			Broadcast.Singleton.RpcAddElement(broadcast.Replace("%nick", fp.Nickname), FriendlyFireConfig.BroadcastTime, Broadcast.BroadcastFlags.Normal);
		}
		if (webhook)
		{
			new Thread(delegate
			{
				CheaterReport.SubmitReport("Friendly Fire Detector", fp.LoggedNameFromFootprint(), "Friendly fire has been detected. Detector: " + detector + ".", fp.NetId, "Friendly Fire Detector", fp.Nickname, true);
			})
			{
				Priority = ThreadPriority.BelowNormal,
				Name = "TK Reporter",
				IsBackground = true
			}.Start();
		}
		switch (action)
		{
		case FriendlyFireAction.Kill:
			if (fp.Hub != null)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Detector, string.Concat(new string[]
				{
					fp.Hub.LoggedNameFromRefHub(),
					" playing as ",
					text,
					" has been automatically killed for teamkilling. Detector: ",
					detector,
					"."
				}), ServerLogs.ServerLogType.Teamkill, false);
				fp.Hub.playerStats.DealDamage(new UniversalDamageHandler(-1f, DeathTranslations.FriendlyFireDetector, null));
				return;
			}
			break;
		case FriendlyFireAction.Kick:
			if (fp.Hub != null)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Detector, string.Concat(new string[]
				{
					fp.Hub.LoggedNameFromRefHub(),
					" playing as ",
					text,
					" has been automatically kicked for teamkilling. Detector: ",
					detector,
					"."
				}), ServerLogs.ServerLogType.Teamkill, false);
				BanPlayer.KickUser(fp.Hub, banReason);
				return;
			}
			break;
		case FriendlyFireAction.Ban:
			ServerLogs.AddLog(ServerLogs.Modules.Detector, string.Format("{0} playing as {1} has been automatically banned for teamkilling. Detector: {2}. Duration: {3} seconds.", new object[]
			{
				fp.LoggedNameFromFootprint(),
				text,
				detector,
				banDuration
			}), ServerLogs.ServerLogType.Teamkill, false);
			BanPlayer.BanUser(fp, banReason, banDuration);
			break;
		default:
			return;
		}
	}

	protected bool _triggered;

	private readonly ReferenceHub _hub;
}
