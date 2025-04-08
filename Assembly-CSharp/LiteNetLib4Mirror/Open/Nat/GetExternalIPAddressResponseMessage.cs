using System;
using System.Net;
using System.Xml;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class GetExternalIPAddressResponseMessage : ResponseMessageBase
	{
		public GetExternalIPAddressResponseMessage(XmlDocument response, string serviceType)
			: base(response, serviceType, "GetExternalIPAddressResponseMessage")
		{
			IPAddress ipaddress;
			if (IPAddress.TryParse(base.GetNode().GetXmlElementText("NewExternalIPAddress"), out ipaddress))
			{
				this.ExternalIPAddress = ipaddress;
			}
		}

		public IPAddress ExternalIPAddress { get; private set; }
	}
}
