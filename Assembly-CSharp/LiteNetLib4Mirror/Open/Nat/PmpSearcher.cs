using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class PmpSearcher : Searcher
	{
		internal PmpSearcher(IIPAddressesProvider ipprovider)
		{
			this._ipprovider = ipprovider;
			this._timeout = 250;
			this.CreateSocketsAndAddGateways();
		}

		private void CreateSocketsAndAddGateways()
		{
			this.UdpClients = new List<UdpClient>();
			this._gatewayLists = new Dictionary<UdpClient, IEnumerable<IPEndPoint>>();
			try
			{
				List<IPEndPoint> list = (from ip in this._ipprovider.GatewayAddresses()
					select new IPEndPoint(ip, 5351)).ToList<IPEndPoint>();
				if (!list.Any<IPEndPoint>())
				{
					list.AddRange(from ip in this._ipprovider.DnsAddresses()
						select new IPEndPoint(ip, 5351));
				}
				if (list.Any<IPEndPoint>())
				{
					foreach (IPAddress ipaddress in this._ipprovider.UnicastAddresses())
					{
						UdpClient udpClient;
						try
						{
							udpClient = new UdpClient(new IPEndPoint(ipaddress, 0));
						}
						catch (SocketException)
						{
							continue;
						}
						this._gatewayLists.Add(udpClient, list);
						this.UdpClients.Add(udpClient);
					}
				}
			}
			catch (Exception ex)
			{
				TraceSource traceSource = NatDiscoverer.TraceSource;
				string text = "There was a problem finding gateways: ";
				Exception ex2 = ex;
				traceSource.LogError(text + ((ex2 != null) ? ex2.ToString() : null), Array.Empty<object>());
			}
		}

		protected override void Discover(UdpClient client, CancellationToken cancelationToken)
		{
			this.NextSearch = DateTime.UtcNow.AddMilliseconds((double)this._timeout);
			this._timeout *= 2;
			if (this._timeout >= 3000)
			{
				this._timeout = 250;
				this.NextSearch = DateTime.UtcNow.AddSeconds(10.0);
				return;
			}
			byte[] array = new byte[2];
			foreach (IPEndPoint ipendPoint in this._gatewayLists[client])
			{
				if (cancelationToken.IsCancellationRequested)
				{
					break;
				}
				client.Send(array, array.Length, ipendPoint);
			}
		}

		private bool IsSearchAddress(IPAddress address)
		{
			return this._gatewayLists.Values.SelectMany((IEnumerable<IPEndPoint> x) => x).Any((IPEndPoint x) => x.Address.Equals(address));
		}

		public override NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
		{
			if (!this.IsSearchAddress(endpoint.Address) || response.Length != 12 || response[0] != 0 || response[1] != 128)
			{
				return null;
			}
			int num = (int)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 2));
			if (num != 0)
			{
				NatDiscoverer.TraceSource.LogError("Non zero error: {0}", new object[] { num });
			}
			IPAddress ipaddress = new IPAddress(new byte[]
			{
				response[8],
				response[9],
				response[10],
				response[11]
			});
			this._timeout = 250;
			return new PmpNatDevice(endpoint.Address, ipaddress);
		}

		private readonly IIPAddressesProvider _ipprovider;

		private Dictionary<UdpClient, IEnumerable<IPEndPoint>> _gatewayLists;

		private int _timeout;
	}
}
