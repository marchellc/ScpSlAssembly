using System;
using UnityEngine;

internal class WindowFriendlyFireDetector : FriendlyFireDetector
{
	internal WindowFriendlyFireDetector(ReferenceHub hub)
		: base(hub)
	{
	}

	public override bool RegisterDamage(float damage)
	{
		if (!FriendlyFireConfig.WindowEnabled || this._triggered)
		{
			return false;
		}
		if (Time.unscaledTime > this._lastReset + FriendlyFireConfig.Window)
		{
			this.Reset();
			this._lastReset = Time.unscaledTime;
		}
		base.RegisterDamage(damage);
		if (FriendlyFireConfig.WindowDamageThreshold > 0U && base.Damage >= FriendlyFireConfig.WindowDamageThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.WindowAction, "Window", ref FriendlyFireConfig.WindowBanTime, ref FriendlyFireConfig.WindowBanReason, ref FriendlyFireConfig.WindowKillReason, ref FriendlyFireConfig.WindowAdminMessage, ref FriendlyFireConfig.WindowBroadcastMessage, ref FriendlyFireConfig.WindowWebhook);
			return true;
		}
		return false;
	}

	public override bool RegisterKill()
	{
		if (!FriendlyFireConfig.WindowEnabled || this._triggered)
		{
			return false;
		}
		if (Time.unscaledTime > this._lastReset + FriendlyFireConfig.Window)
		{
			this.Reset();
			this._lastReset = Time.unscaledTime;
		}
		base.RegisterKill();
		if (FriendlyFireConfig.WindowKillThreshold > 0U && base.Kills >= FriendlyFireConfig.WindowKillThreshold)
		{
			base.TakeAction(ref FriendlyFireConfig.WindowAction, "Window", ref FriendlyFireConfig.WindowBanTime, ref FriendlyFireConfig.WindowBanReason, ref FriendlyFireConfig.WindowKillReason, ref FriendlyFireConfig.WindowAdminMessage, ref FriendlyFireConfig.WindowBroadcastMessage, ref FriendlyFireConfig.WindowWebhook);
			return true;
		}
		return false;
	}

	private const string Detector = "Window";

	private float _lastReset = Time.unscaledTime;
}
