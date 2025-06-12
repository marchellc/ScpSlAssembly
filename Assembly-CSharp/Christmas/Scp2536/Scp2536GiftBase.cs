using System.Collections.Generic;
using GameCore;

namespace Christmas.Scp2536;

public abstract class Scp2536GiftBase
{
	public readonly HashSet<ReferenceHub> ObtainedBy = new HashSet<ReferenceHub>();

	public abstract UrgencyLevel Urgency { get; }

	internal virtual bool IgnoredByRandomness => false;

	public virtual float SecondsUntilAvailable => 0f;

	public virtual float SecondsUntilUnavailable => 0f;

	public virtual bool CanBeGranted(ReferenceHub hub)
	{
		if (this.ObtainedBy.Contains(hub))
		{
			return false;
		}
		double totalSeconds = RoundStart.RoundLength.TotalSeconds;
		bool flag = this.SecondsUntilUnavailable > 0f && totalSeconds < (double)this.SecondsUntilUnavailable;
		if (totalSeconds >= (double)this.SecondsUntilAvailable)
		{
			return !flag;
		}
		return false;
	}

	public abstract void ServerGrant(ReferenceHub hub);

	public virtual void Reset()
	{
		this.ObtainedBy.Clear();
	}
}
