using System;

internal class RoundFriendlyFireDetector : FriendlyFireDetector
{
	internal RoundFriendlyFireDetector(ReferenceHub hub)
		: base(hub)
	{
	}

	public override bool RegisterDamage(float damage)
	{
		if (!FriendlyFireConfig.RoundEnabled || this._triggered)
		{
			return false;
		}
		base.RegisterDamage(damage);
		if (FriendlyFireConfig.RoundDamageThreshold > 0U && base.Damage >= FriendlyFireConfig.RoundDamageThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.RoundAction, "Round", ref FriendlyFireConfig.RoundBanTime, ref FriendlyFireConfig.RoundBanReason, ref FriendlyFireConfig.RoundKillReason, ref FriendlyFireConfig.RoundAdminMessage, ref FriendlyFireConfig.RoundBroadcastMessage, ref FriendlyFireConfig.RoundWebhook);
			return true;
		}
		return false;
	}

	public override bool RegisterKill()
	{
		if (!FriendlyFireConfig.RoundEnabled || this._triggered)
		{
			return false;
		}
		base.RegisterKill();
		if (FriendlyFireConfig.RoundKillThreshold > 0U && base.Kills >= FriendlyFireConfig.RoundKillThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.RoundAction, "Round", ref FriendlyFireConfig.RoundBanTime, ref FriendlyFireConfig.RoundBanReason, ref FriendlyFireConfig.RoundKillReason, ref FriendlyFireConfig.RoundAdminMessage, ref FriendlyFireConfig.RoundBroadcastMessage, ref FriendlyFireConfig.RoundWebhook);
			return true;
		}
		return false;
	}

	private const string Detector = "Round";
}
