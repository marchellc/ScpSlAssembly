using System;
using System.Collections.Generic;
using GameCore;

namespace Christmas.Scp2536
{
	public abstract class Scp2536GiftBase
	{
		public abstract UrgencyLevel Urgency { get; }

		internal virtual bool IgnoredByRandomness
		{
			get
			{
				return false;
			}
		}

		public virtual float SecondsUntilAvailable
		{
			get
			{
				return 0f;
			}
		}

		public virtual float SecondsUntilUnavailable
		{
			get
			{
				return 0f;
			}
		}

		public virtual bool CanBeGranted(ReferenceHub hub)
		{
			if (this.ObtainedBy.Contains(hub))
			{
				return false;
			}
			double totalSeconds = RoundStart.RoundLength.TotalSeconds;
			bool flag = this.SecondsUntilUnavailable > 0f && totalSeconds < (double)this.SecondsUntilUnavailable;
			return totalSeconds >= (double)this.SecondsUntilAvailable && !flag;
		}

		public abstract void ServerGrant(ReferenceHub hub);

		public virtual void Reset()
		{
			this.ObtainedBy.Clear();
		}

		public readonly HashSet<ReferenceHub> ObtainedBy = new HashSet<ReferenceHub>();
	}
}
