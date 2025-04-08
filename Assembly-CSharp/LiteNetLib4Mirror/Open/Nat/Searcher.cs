using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal abstract class Searcher
	{
		public async Task<IEnumerable<NatDevice>> Search(CancellationToken cancelationToken)
		{
			await Task.Factory.StartNew(delegate(object _)
			{
				NatDiscoverer.TraceSource.LogInfo("Searching for: {0}", new object[] { this.GetType().Name });
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
					NatDiscoverer.TraceSource.LogError("Error searching {0} - Details:", new object[] { base.GetType().Name });
					NatDiscoverer.TraceSource.LogError(ex.ToString(), Array.Empty<object>());
				}
			}
		}

		private void Receive(CancellationToken cancelationToken)
		{
			foreach (UdpClient udpClient in this.UdpClients.Where((UdpClient x) => x.Available > 0))
			{
				if (cancelationToken.IsCancellationRequested)
				{
					break;
				}
				IPAddress address = ((IPEndPoint)udpClient.Client.LocalEndPoint).Address;
				IPEndPoint ipendPoint = new IPEndPoint(IPAddress.None, 0);
				byte[] array = udpClient.Receive(ref ipendPoint);
				NatDevice natDevice = this.AnalyseReceivedResponse(address, array, ipendPoint);
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
			EventHandler<DeviceEventArgs> deviceFound = this.DeviceFound;
			if (deviceFound == null)
			{
				return;
			}
			deviceFound(this, new DeviceEventArgs(device));
		}

		private readonly List<NatDevice> _devices = new List<NatDevice>();

		protected List<UdpClient> UdpClients;

		public EventHandler<DeviceEventArgs> DeviceFound;

		internal DateTime NextSearch = DateTime.UtcNow;
	}
}
