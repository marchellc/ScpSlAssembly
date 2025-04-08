using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LiteNetLib.Utils;
using LiteNetLib4Mirror.Open.Nat;
using UnityEngine;

namespace Mirror.LiteNetLib4Mirror
{
	public static class LiteNetLib4MirrorUtils
	{
		public static bool UpnpFailed { get; private set; }

		public static IPAddress ExternalIp { get; private set; }

		public static string ToBase64(string text)
		{
			return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
		}

		public static string FromBase64(string text)
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(text));
		}

		public static NetDataWriter ReusePut(NetDataWriter writer, string text, ref string lastText)
		{
			if (text != lastText)
			{
				lastText = text;
				writer.Reset();
				writer.Put(LiteNetLib4MirrorUtils.ToBase64(text));
			}
			return writer;
		}

		public static NetDataWriter ReusePutDiscovery(NetDataWriter writer, string text, ref string lastText)
		{
			if (LiteNetLib4MirrorUtils.ApplicationName + text != lastText)
			{
				lastText = LiteNetLib4MirrorUtils.ApplicationName + text;
				writer.Reset();
				writer.Put(LiteNetLib4MirrorUtils.ApplicationName);
				writer.Put(LiteNetLib4MirrorUtils.ToBase64(text));
			}
			return writer;
		}

		public static IPAddress Parse(string address)
		{
			uint num = <PrivateImplementationDetails>.ComputeStringHash(address);
			if (num <= 364823692U)
			{
				if (num != 22050618U)
				{
					if (num != 144953630U)
					{
						if (num != 364823692U)
						{
							goto IL_00E2;
						}
						if (!(address == "::1"))
						{
							goto IL_00E2;
						}
						goto IL_00DC;
					}
					else if (!(address == "127.0.0.1"))
					{
						goto IL_00E2;
					}
				}
				else if (!(address == "localhost"))
				{
					goto IL_00E2;
				}
				return IPAddress.Loopback;
			}
			if (num <= 3629563401U)
			{
				if (num != 2550542581U)
				{
					if (num != 3629563401U)
					{
						goto IL_00E2;
					}
					if (!(address == "0.0.0.0"))
					{
						goto IL_00E2;
					}
					return IPAddress.Any;
				}
				else if (!(address == "::"))
				{
					goto IL_00E2;
				}
			}
			else if (num != 4095679530U)
			{
				if (num != 4112457149U)
				{
					goto IL_00E2;
				}
				if (!(address == "0:0:0:0:0:0:0:0"))
				{
					goto IL_00E2;
				}
			}
			else
			{
				if (!(address == "0:0:0:0:0:0:0:1"))
				{
					goto IL_00E2;
				}
				goto IL_00DC;
			}
			return IPAddress.IPv6Any;
			IL_00DC:
			return IPAddress.IPv6Loopback;
			IL_00E2:
			IPAddress ipaddress;
			if (IPAddress.TryParse(address, out ipaddress))
			{
				return ipaddress;
			}
			IPAddress[] hostAddresses = Dns.GetHostAddresses(address);
			if (LiteNetLib4MirrorTransport.Singleton.ipv6Enabled)
			{
				return (LiteNetLib4MirrorUtils.FirstAddressOfType(hostAddresses, AddressFamily.InterNetworkV6) ?? LiteNetLib4MirrorUtils.FirstAddressOfType(hostAddresses, AddressFamily.InterNetwork)) ?? hostAddresses[0];
			}
			return LiteNetLib4MirrorUtils.FirstAddressOfType(hostAddresses, AddressFamily.InterNetwork) ?? hostAddresses[0];
		}

		public static IPEndPoint Parse(string address, ushort port)
		{
			return new IPEndPoint(LiteNetLib4MirrorUtils.Parse(address), (int)port);
		}

		private static IPAddress FirstAddressOfType(IPAddress[] addresses, AddressFamily type)
		{
			foreach (IPAddress ipaddress in addresses)
			{
				if (ipaddress.AddressFamily == type)
				{
					return ipaddress;
				}
			}
			return null;
		}

		public static ushort GetFirstFreePort(params ushort[] ports)
		{
			if (ports == null || ports.Length == 0)
			{
				throw new Exception("No ports provided");
			}
			ushort num = ports.Except(Array.ConvertAll<IPEndPoint, ushort>(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners(), (IPEndPoint p) => (ushort)p.Port)).FirstOrDefault<ushort>();
			if (num == 0)
			{
				throw new Exception("No free port!");
			}
			return num;
		}

		public static void ForwardPort(NetworkProtocolType networkProtocolType = NetworkProtocolType.Udp, int milisecondsDelay = 10000)
		{
			LiteNetLib4MirrorUtils.ForwardPortInternalAsync(LiteNetLib4MirrorTransport.Singleton.port, milisecondsDelay, networkProtocolType);
		}

		public static void ForwardPort(ushort port, NetworkProtocolType networkProtocolType = NetworkProtocolType.Udp, int milisecondsDelay = 10000)
		{
			LiteNetLib4MirrorUtils.ForwardPortInternalAsync(port, milisecondsDelay, networkProtocolType);
		}

		private static async Task ForwardPortInternalAsync(ushort port, int milisecondsDelay, NetworkProtocolType networkProtocolType = NetworkProtocolType.Udp)
		{
			try
			{
				if (LiteNetLib4MirrorUtils.LastForwardedPort != port && !LiteNetLib4MirrorUtils.UpnpFailed)
				{
					if (LiteNetLib4MirrorUtils.LastForwardedPort != 0)
					{
						NatDiscoverer.ReleaseAll();
					}
					NatDiscoverer natDiscoverer = new NatDiscoverer();
					NatDevice device;
					using (CancellationTokenSource cts = new CancellationTokenSource(milisecondsDelay))
					{
						NatDevice natDevice = await natDiscoverer.DiscoverDeviceAsync(PortMapper.Pmp | PortMapper.Upnp, cts).ConfigureAwait(false);
						device = natDevice;
					}
					CancellationTokenSource cts = null;
					LiteNetLib4MirrorUtils.ExternalIp = await device.GetExternalIPAsync();
					await device.CreatePortMapAsync(new Mapping(networkProtocolType, IPAddress.None, (int)port, (int)port, 0, LiteNetLib4MirrorUtils.ApplicationName)).ConfigureAwait(false);
					LiteNetLib4MirrorUtils.LastForwardedPort = port;
					Debug.Log("Port " + port.ToString() + " forwarded successfully!");
					device = null;
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("UPnP failed: " + ex.Message);
				LiteNetLib4MirrorUtils.UpnpFailed = true;
			}
		}

		internal static ushort LastForwardedPort;

		internal static readonly string ApplicationName = Application.productName;
	}
}
