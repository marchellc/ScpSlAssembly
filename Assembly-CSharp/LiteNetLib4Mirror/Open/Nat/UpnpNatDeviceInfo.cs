using System;
using System.Net;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class UpnpNatDeviceInfo
	{
		public UpnpNatDeviceInfo(IPAddress localAddress, Uri locationUri, string serviceControlUrl, string serviceType)
		{
			this.LocalAddress = localAddress;
			this.ServiceType = serviceType;
			this.HostEndPoint = new IPEndPoint(IPAddress.Parse(locationUri.Host), locationUri.Port);
			if (Uri.IsWellFormedUriString(serviceControlUrl, UriKind.Absolute))
			{
				Uri uri = new Uri(serviceControlUrl);
				IPEndPoint hostEndPoint = this.HostEndPoint;
				serviceControlUrl = uri.PathAndQuery;
				NatDiscoverer.TraceSource.LogInfo("{0}: Absolute URI detected. Host address is now: {1}", new object[] { hostEndPoint, this.HostEndPoint });
				NatDiscoverer.TraceSource.LogInfo("{0}: New control url: {1}", new object[] { this.HostEndPoint, serviceControlUrl });
			}
			UriBuilder uriBuilder = new UriBuilder("http", locationUri.Host, locationUri.Port);
			this.ServiceControlUri = new Uri(uriBuilder.Uri, serviceControlUrl);
		}

		public IPEndPoint HostEndPoint { get; private set; }

		public IPAddress LocalAddress { get; private set; }

		public string ServiceType { get; private set; }

		public Uri ServiceControlUri { get; private set; }
	}
}
