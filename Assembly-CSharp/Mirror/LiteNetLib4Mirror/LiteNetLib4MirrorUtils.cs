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

namespace Mirror.LiteNetLib4Mirror;

public static class LiteNetLib4MirrorUtils
{
	internal static ushort LastForwardedPort;

	internal static readonly string ApplicationName;

	public static bool UpnpFailed { get; private set; }

	public static IPAddress ExternalIp { get; private set; }

	static LiteNetLib4MirrorUtils()
	{
		LiteNetLib4MirrorUtils.ApplicationName = Application.productName;
	}

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
		switch (address)
		{
		case "0.0.0.0":
			return IPAddress.Any;
		case "0:0:0:0:0:0:0:0":
		case "::":
			return IPAddress.IPv6Any;
		case "localhost":
		case "127.0.0.1":
			return IPAddress.Loopback;
		case "0:0:0:0:0:0:0:1":
		case "::1":
			return IPAddress.IPv6Loopback;
		default:
		{
			if (IPAddress.TryParse(address, out var address2))
			{
				return address2;
			}
			IPAddress[] hostAddresses = Dns.GetHostAddresses(address);
			if (LiteNetLib4MirrorTransport.Singleton.ipv6Enabled)
			{
				object obj = LiteNetLib4MirrorUtils.FirstAddressOfType(hostAddresses, AddressFamily.InterNetworkV6) ?? LiteNetLib4MirrorUtils.FirstAddressOfType(hostAddresses, AddressFamily.InterNetwork);
				if (obj == null)
				{
					obj = hostAddresses[0];
				}
				return (IPAddress)obj;
			}
			return LiteNetLib4MirrorUtils.FirstAddressOfType(hostAddresses, AddressFamily.InterNetwork) ?? hostAddresses[0];
		}
		}
	}

	public static IPEndPoint Parse(string address, ushort port)
	{
		return new IPEndPoint(LiteNetLib4MirrorUtils.Parse(address), port);
	}

	private static IPAddress FirstAddressOfType(IPAddress[] addresses, AddressFamily type)
	{
		foreach (IPAddress iPAddress in addresses)
		{
			if (iPAddress.AddressFamily == type)
			{
				return iPAddress;
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
		ushort num = ports.Except(Array.ConvertAll(IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners(), (IPEndPoint p) => (ushort)p.Port)).FirstOrDefault();
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
		_ = 2;
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
					device = await natDiscoverer.DiscoverDeviceAsync(PortMapper.Pmp | PortMapper.Upnp, cts).ConfigureAwait(continueOnCapturedContext: false);
				}
				LiteNetLib4MirrorUtils.ExternalIp = await device.GetExternalIPAsync();
				await device.CreatePortMapAsync(new Mapping(networkProtocolType, IPAddress.None, port, port, 0, LiteNetLib4MirrorUtils.ApplicationName)).ConfigureAwait(continueOnCapturedContext: false);
				LiteNetLib4MirrorUtils.LastForwardedPort = port;
				Debug.Log("Port " + port + " forwarded successfully!");
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("UPnP failed: " + ex.Message);
			LiteNetLib4MirrorUtils.UpnpFailed = true;
		}
	}
}
