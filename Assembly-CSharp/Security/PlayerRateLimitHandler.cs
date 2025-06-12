using Mirror;

namespace Security;

public class PlayerRateLimitHandler : NetworkBehaviour
{
	public RateLimit[] RateLimits;

	private void Awake()
	{
		this.RateLimits = RateLimitCreator.CreateRateLimit(base.connectionToClient, base.isServer && base.isLocalPlayer);
	}

	public override bool Weaved()
	{
		return true;
	}
}
