using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LiteNetLib4Mirror.Open.Nat;

internal class PmpSearcher : Searcher
{
	private readonly IIPAddressesProvider _ipprovider;

	private Dictionary<UdpClient, IEnumerable<IPEndPoint>> _gatewayLists;

	private int _timeout;

	internal PmpSearcher(IIPAddressesProvider ipprovider)
	{
		this._ipprovider = ipprovider;
		this._timeout = 250;
		this.CreateSocketsAndAddGateways();
	}

	private void CreateSocketsAndAddGateways()
	{
		base.UdpClients = new List<UdpClient>();
		this._gatewayLists = new Dictionary<UdpClient, IEnumerable<IPEndPoint>>();
		try
		{
			List<IPEndPoint> list = (from ip in this._ipprovider.GatewayAddresses()
				select new IPEndPoint(ip, 5351)).ToList();
			if (!list.Any())
			{
				list.AddRange(from ip in this._ipprovider.DnsAddresses()
					select new IPEndPoint(ip, 5351));
			}
			if (!list.Any())
			{
				return;
			}
			foreach (IPAddress item in this._ipprovider.UnicastAddresses())
			{
				UdpClient udpClient;
				try
				{
					udpClient = new UdpClient(new IPEndPoint(item, 0));
				}
				catch (SocketException)
				{
					continue;
				}
				this._gatewayLists.Add(udpClient, list);
				base.UdpClients.Add(udpClient);
			}
		}
		catch (Exception ex2)
		{
			NatDiscoverer.TraceSource.LogError("There was a problem finding gateways: " + ex2);
		}
	}

	protected override void Discover(UdpClient client, CancellationToken cancelationToken)
	{
		base.NextSearch = DateTime.UtcNow.AddMilliseconds(this._timeout);
		this._timeout *= 2;
		if (this._timeout >= 3000)
		{
			this._timeout = 250;
			base.NextSearch = DateTime.UtcNow.AddSeconds(10.0);
			return;
		}
		byte[] array = new byte[2];
		foreach (IPEndPoint item in this._gatewayLists[client])
		{
			if (cancelationToken.IsCancellationRequested)
			{
				break;
			}
			client.Send(array, array.Length, item);
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
		int num = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 2));
		if (num != 0)
		{
			NatDiscoverer.TraceSource.LogError("Non zero error: {0}", num);
		}
		IPAddress publicAddress = new IPAddress(new byte[4]
		{
			response[8],
			response[9],
			response[10],
			response[11]
		});
		this._timeout = 250;
		return new PmpNatDevice(endpoint.Address, publicAddress);
	}
}
