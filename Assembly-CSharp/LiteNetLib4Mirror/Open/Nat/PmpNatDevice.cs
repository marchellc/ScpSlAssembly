using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal sealed class PmpNatDevice : NatDevice
	{
		internal PmpNatDevice(IPAddress localAddress, IPAddress publicAddress)
		{
			this.LocalAddress = localAddress;
			this._publicAddress = publicAddress;
		}

		internal IPAddress LocalAddress { get; private set; }

		public override async Task CreatePortMapAsync(Mapping mapping)
		{
			await this.InternalCreatePortMapAsync(mapping, true).TimeoutAfter(TimeSpan.FromSeconds(4.0));
			base.RegisterMapping(mapping);
		}

		public override async Task DeletePortMapAsync(Mapping mapping)
		{
			await this.InternalCreatePortMapAsync(mapping, false).TimeoutAfter(TimeSpan.FromSeconds(4.0));
			base.UnregisterMapping(mapping);
		}

		public override Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
			throw new NotSupportedException();
		}

		public override Task<IPAddress> GetExternalIPAsync()
		{
			return Task.Run<IPAddress>(() => this._publicAddress).TimeoutAfter(TimeSpan.FromSeconds(4.0));
		}

		public override Task<Mapping> GetSpecificMappingAsync(NetworkProtocolType networkProtocolType, int port)
		{
			throw new NotSupportedException("NAT-PMP does not specify a way to get a specific port map");
		}

		private async Task<Mapping> InternalCreatePortMapAsync(Mapping mapping, bool create)
		{
			List<byte> list = new List<byte>();
			list.Add(0);
			list.Add((mapping.NetworkProtocolType == NetworkProtocolType.Tcp) ? 2 : 1);
			list.Add(0);
			list.Add(0);
			list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)mapping.PrivatePort)));
			list.AddRange(BitConverter.GetBytes(create ? IPAddress.HostToNetworkOrder((short)mapping.PublicPort) : 0));
			list.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(mapping.Lifetime)));
			try
			{
				byte[] buffer = list.ToArray();
				int attempt = 0;
				int delay = 250;
				using (UdpClient udpClient = new UdpClient())
				{
					this.CreatePortMapListen(udpClient, mapping);
					while (attempt < 9)
					{
						await udpClient.SendAsync(buffer, buffer.Length, new IPEndPoint(this.LocalAddress, 5351));
						attempt++;
						delay *= 2;
						Thread.Sleep(delay);
					}
				}
				UdpClient udpClient = null;
				buffer = null;
			}
			catch (Exception ex)
			{
				string text = string.Format("Failed to {0} portmap (protocol={1}, private port={2})", create ? "create" : "delete", mapping.NetworkProtocolType, mapping.PrivatePort);
				NatDiscoverer.TraceSource.LogError(text, Array.Empty<object>());
				throw new MappingException(text, ex as MappingException);
			}
			return mapping;
		}

		private void CreatePortMapListen(UdpClient udpClient, Mapping mapping)
		{
			IPEndPoint ipendPoint = new IPEndPoint(this.LocalAddress, 5351);
			byte[] array;
			do
			{
				array = udpClient.Receive(ref ipendPoint);
			}
			while (array.Length < 16 || array[0] != 0);
			int num = (int)(array[1] & 127);
			NetworkProtocolType networkProtocolType = NetworkProtocolType.Tcp;
			if (num == 1)
			{
				networkProtocolType = NetworkProtocolType.Udp;
			}
			short num2 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(array, 2));
			IPAddress.NetworkToHostOrder(BitConverter.ToInt32(array, 4));
			short num3 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(array, 8));
			short num4 = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(array, 10));
			uint num5 = (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(array, 12));
			if (num3 < 0 || num4 < 0 || num2 != 0)
			{
				string[] array2 = new string[] { "Success", "Unsupported Version", "Not Authorized/Refused (e.g. box supports mapping, but user has turned feature off)", "Network Failure (e.g. NAT box itself has not obtained a DHCP lease)", "Out of resources (NAT box cannot create any more mappings at this time)", "Unsupported opcode" };
				throw new MappingException((int)num2, array2[(int)num2]);
			}
			if (num5 == 0U)
			{
				return;
			}
			mapping.PublicPort = (int)num4;
			mapping.NetworkProtocolType = networkProtocolType;
			mapping.Expiration = DateTime.Now.AddSeconds(num5);
		}

		public override string ToString()
		{
			return string.Format("Local Address: {0}\nPublic IP: {1}\nLast Seen: {2}", this.LocalAddress, this._publicAddress, base.LastSeen);
		}

		private readonly IPAddress _publicAddress;
	}
}
