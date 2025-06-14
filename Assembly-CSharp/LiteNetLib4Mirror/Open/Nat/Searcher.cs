using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiteNetLib4Mirror.Open.Nat;

internal abstract class Searcher
{
	private readonly List<NatDevice> _devices = new List<NatDevice>();

	protected List<UdpClient> UdpClients;

	public EventHandler<DeviceEventArgs> DeviceFound;

	internal DateTime NextSearch = DateTime.UtcNow;

	public async Task<IEnumerable<NatDevice>> Search(CancellationToken cancelationToken)
	{
		await Task.Factory.StartNew(delegate
		{
			NatDiscoverer.TraceSource.LogInfo("Searching for: {0}", base.GetType().Name);
			while (!cancelationToken.IsCancellationRequested)
			{
				this.Discover(cancelationToken);
				this.Receive(cancelationToken);
			}
			this.CloseUdpClients();
		}, null, cancelationToken);
		return this._devices;
	}

	private void Discover(CancellationToken cancelationToken)
	{
		if (DateTime.UtcNow < this.NextSearch)
		{
			return;
		}
		foreach (UdpClient udpClient in this.UdpClients)
		{
			try
			{
				this.Discover(udpClient, cancelationToken);
			}
			catch (Exception ex)
			{
				NatDiscoverer.TraceSource.LogError("Error searching {0} - Details:", base.GetType().Name);
				NatDiscoverer.TraceSource.LogError(ex.ToString());
			}
		}
	}

	private void Receive(CancellationToken cancelationToken)
	{
		foreach (UdpClient item in this.UdpClients.Where((UdpClient x) => x.Available > 0))
		{
			if (cancelationToken.IsCancellationRequested)
			{
				break;
			}
			IPAddress address = ((IPEndPoint)item.Client.LocalEndPoint).Address;
			IPEndPoint remoteEP = new IPEndPoint(IPAddress.None, 0);
			byte[] response = item.Receive(ref remoteEP);
			NatDevice natDevice = this.AnalyseReceivedResponse(address, response, remoteEP);
			if (natDevice != null)
			{
				this.RaiseDeviceFound(natDevice);
			}
		}
	}

	protected abstract void Discover(UdpClient client, CancellationToken cancelationToken);

	public abstract NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint);

	public void CloseUdpClients()
	{
		foreach (UdpClient udpClient in this.UdpClients)
		{
			udpClient.Close();
		}
	}

	private void RaiseDeviceFound(NatDevice device)
	{
		this._devices.Add(device);
		this.DeviceFound?.Invoke(this, new DeviceEventArgs(device));
	}
}
