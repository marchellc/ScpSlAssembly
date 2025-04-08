using System;
using UnityEngine;

internal class RespawnFriendlyFireDetector : FriendlyFireDetector
{
	internal RespawnFriendlyFireDetector(ReferenceHub hub)
		: base(hub)
	{
	}

	public override bool RegisterDamage(float damage)
	{
		if (!FriendlyFireConfig.RespawnEnabled || this._triggered || Time.unscaledTime > this._lastReset + FriendlyFireConfig.RespawnWindow)
		{
			return false;
		}
		base.RegisterDamage(damage);
		if (FriendlyFireConfig.RespawnDamageThreshold > 0U && base.Damage >= FriendlyFireConfig.RespawnDamageThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.RespawnAction, "Respawn", ref FriendlyFireConfig.RespawnBanTime, ref FriendlyFireConfig.RespawnBanReason, ref FriendlyFireConfig.RespawnKillReason, ref FriendlyFireConfig.RespawnAdminMessage, ref FriendlyFireConfig.RespawnBroadcastMessage, ref FriendlyFireConfig.RespawnWebhook);
			return true;
		}
		return false;
	}

	public override bool RegisterKill()
	{
		if (!FriendlyFireConfig.RespawnEnabled || this._triggered || Time.unscaledTime > this._lastReset + FriendlyFireConfig.RespawnWindow)
		{
			return false;
		}
		base.RegisterKill();
		if (FriendlyFireConfig.RespawnKillThreshold > 0U && base.Kills >= FriendlyFireConfig.RespawnKillThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.RespawnAction, "Respawn", ref FriendlyFireConfig.RespawnBanTime, ref FriendlyFireConfig.RespawnBanReason, ref FriendlyFireConfig.RespawnKillReason, ref FriendlyFireConfig.RespawnAdminMessage, ref FriendlyFireConfig.RespawnBroadcastMessage, ref FriendlyFireConfig.RespawnWebhook);
			return true;
		}
		return false;
	}

	public override void Reset()
	{
		base.Reset();
		this._lastReset = Time.unscaledTime;
	}

	private const string Detector = "Respawn";

	private float _lastReset = Time.unscaledTime;
}
