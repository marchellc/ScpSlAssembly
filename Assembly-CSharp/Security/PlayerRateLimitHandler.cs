using System;
using Mirror;

namespace Security
{
	public class PlayerRateLimitHandler : NetworkBehaviour
	{
		private void Awake()
		{
			this.RateLimits = RateLimitCreator.CreateRateLimit(base.connectionToClient, base.isServer && base.isLocalPlayer);
		}

		public override bool Weaved()
		{
			return true;
		}

		public RateLimit[] RateLimits;
	}
}
