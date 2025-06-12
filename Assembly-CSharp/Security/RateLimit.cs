using Mirror;
using UnityEngine;

namespace Security;

public class RateLimit
{
	private readonly int _usagesAllowed;

	private uint _usages;

	private readonly float _timeWindow;

	private float _usageTime;

	private readonly NetworkConnection _conn;

	public RateLimit(int usagesAllowed, float timeWindow, NetworkConnection conn = null)
	{
		this._usagesAllowed = usagesAllowed;
		this._timeWindow = timeWindow;
		this._conn = conn;
	}

	public bool CanExecute(bool countUsage = true)
	{
		if (this._usagesAllowed < 0)
		{
			return true;
		}
		if (this._timeWindow >= 0f && Time.fixedUnscaledTime - this._usageTime > this._timeWindow)
		{
			this._usages = 1u;
			this._usageTime = Time.fixedUnscaledTime;
			return true;
		}
		if (this._usages >= this._usagesAllowed)
		{
			if (ServerConsole.RateLimitKick && this._conn != null)
			{
				ServerConsole.Disconnect(this._conn, "Reason: Exceeding rate limit.");
			}
			return false;
		}
		if (countUsage)
		{
			this._usages++;
		}
		return true;
	}

	public void Reset()
	{
		this._usages = 0u;
		this._usageTime = Time.fixedUnscaledTime;
	}
}
