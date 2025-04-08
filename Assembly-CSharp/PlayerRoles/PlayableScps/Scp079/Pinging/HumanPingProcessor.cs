using System;

namespace PlayerRoles.PlayableScps.Scp079.Pinging
{
	public class HumanPingProcessor : IPingProcessor
	{
		public float Range
		{
			get
			{
				return 60f;
			}
		}
	}
}
