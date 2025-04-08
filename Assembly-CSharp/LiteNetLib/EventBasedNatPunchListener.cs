using System;
using System.Net;

namespace LiteNetLib
{
	public class EventBasedNatPunchListener : INatPunchListener
	{
		public event EventBasedNatPunchListener.OnNatIntroductionRequest NatIntroductionRequest;

		public event EventBasedNatPunchListener.OnNatIntroductionSuccess NatIntroductionSuccess;

		void INatPunchListener.OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
		{
			if (this.NatIntroductionRequest != null)
			{
				this.NatIntroductionRequest(localEndPoint, remoteEndPoint, token);
			}
		}

		void INatPunchListener.OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
		{
			if (this.NatIntroductionSuccess != null)
			{
				this.NatIntroductionSuccess(targetEndPoint, type, token);
			}
		}

		public delegate void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token);

		public delegate void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token);
	}
}
