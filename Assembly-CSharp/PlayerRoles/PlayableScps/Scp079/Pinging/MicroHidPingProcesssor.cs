using System;

namespace PlayerRoles.PlayableScps.Scp079.Pinging
{
	public class MicroHidPingProcesssor : IPingProcessor
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
