using System;

namespace PlayerRoles.PlayableScps.Scp079.Pinging
{
	public class DefaultPingProcessor : IPingProcessor
	{
		public float Range
		{
			get
			{
				return 45f;
			}
		}
	}
}
