using System;

internal class LifeFriendlyFireDetector : FriendlyFireDetector
{
	internal LifeFriendlyFireDetector(ReferenceHub hub)
		: base(hub)
	{
	}

	public override bool RegisterDamage(float damage)
	{
		if (!FriendlyFireConfig.LifeEnabled || this._triggered)
		{
			return false;
		}
		base.RegisterDamage(damage);
		if (FriendlyFireConfig.LifeDamageThreshold > 0U && base.Damage >= FriendlyFireConfig.LifeDamageThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.LifeAction, "Life", ref FriendlyFireConfig.LifeBanTime, ref FriendlyFireConfig.LifeBanReason, ref FriendlyFireConfig.LifeKillReason, ref FriendlyFireConfig.LifeAdminMessage, ref FriendlyFireConfig.LifeBroadcastMessage, ref FriendlyFireConfig.LifeWebhook);
			return true;
		}
		return false;
	}

	public override bool RegisterKill()
	{
		if (!FriendlyFireConfig.LifeEnabled || this._triggered)
		{
			return false;
		}
		base.RegisterKill();
		if (FriendlyFireConfig.LifeKillThreshold > 0U && base.Kills >= FriendlyFireConfig.LifeKillThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.LifeAction, "Life", ref FriendlyFireConfig.LifeBanTime, ref FriendlyFireConfig.LifeBanReason, ref FriendlyFireConfig.LifeKillReason, ref FriendlyFireConfig.LifeAdminMessage, ref FriendlyFireConfig.LifeBroadcastMessage, ref FriendlyFireConfig.LifeWebhook);
			return true;
		}
		return false;
	}

	private const string Detector = "Life";
}
