using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace LiteNetLib
{
	public static class NetUtils
	{
		public static IPEndPoint MakeEndPoint(string hostStr, int port)
		{
			return new IPEndPoint(NetUtils.ResolveAddress(hostStr), port);
		}

		public static IPAddress ResolveAddress(string hostStr)
		{
			if (hostStr == "localhost")
			{
				return IPAddress.Loopback;
			}
			IPAddress ipaddress;
			if (!IPAddress.TryParse(hostStr, out ipaddress))
			{
				if (NetManager.IPv6Support)
				{
					ipaddress = NetUtils.ResolveAddress(hostStr, AddressFamily.InterNetworkV6);
				}
				if (ipaddress == null)
				{
					ipaddress = NetUtils.ResolveAddress(hostStr, AddressFamily.InterNetwork);
				}
			}
			if (ipaddress == null)
			{
				throw new ArgumentException("Invalid address: " + hostStr);
			}
			return ipaddress;
		}

		public static IPAddress ResolveAddress(string hostStr, AddressFamily addressFamily)
		{
			foreach (IPAddress ipaddress in Dns.GetHostEntry(hostStr).AddressList)
			{
				if (ipaddress.AddressFamily == addressFamily)
				{
					return ipaddress;
				}
			}
			return null;
		}

		public static List<string> GetLocalIpList(LocalAddrType addrType)
		{
			List<string> list = new List<string>();
			NetUtils.GetLocalIpList(list, addrType);
			return list;
		}

		public static void GetLocalIpList(IList<string> targetList, LocalAddrType addrType)
		{
			bool flag = (addrType & LocalAddrType.IPv4) == LocalAddrType.IPv4;
			bool flag2 = (addrType & LocalAddrType.IPv6) == LocalAddrType.IPv6;
			try
			{
				foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
				{
					if (networkInterface.NetworkInterfaceType != NetworkInterfaceType.Loopback && networkInterface.OperationalStatus == OperationalStatus.Up)
					{
						IPInterfaceProperties ipproperties = networkInterface.GetIPProperties();
						if (ipproperties.GatewayAddresses.Count != 0)
						{
							foreach (UnicastIPAddressInformation unicastIPAddressInformation in ipproperties.UnicastAddresses)
							{
								IPAddress address = unicastIPAddressInformation.Address;
								if ((flag && address.AddressFamily == AddressFamily.InterNetwork) || (flag2 && address.AddressFamily == AddressFamily.InterNetworkV6))
								{
									targetList.Add(address.ToString());
								}
							}
						}
					}
				}
				if (targetList.Count == 0)
				{
					foreach (IPAddress ipaddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
					{
						if ((flag && ipaddress.AddressFamily == AddressFamily.InterNetwork) || (flag2 && ipaddress.AddressFamily == AddressFamily.InterNetworkV6))
						{
							targetList.Add(ipaddress.ToString());
						}
					}
				}
			}
			catch
			{
			}
			if (targetList.Count == 0)
			{
				if (flag)
				{
					targetList.Add("127.0.0.1");
				}
				if (flag2)
				{
					targetList.Add("::1");
				}
			}
		}

		public static string GetLocalIp(LocalAddrType addrType)
		{
			List<string> ipList = NetUtils.IpList;
			string text;
			lock (ipList)
			{
				NetUtils.IpList.Clear();
				NetUtils.GetLocalIpList(NetUtils.IpList, addrType);
				text = ((NetUtils.IpList.Count == 0) ? string.Empty : NetUtils.IpList[0]);
			}
			return text;
		}

		internal static void PrintInterfaceInfos()
		{
			try
			{
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				for (int i = 0; i < allNetworkInterfaces.Length; i++)
				{
					foreach (UnicastIPAddressInformation unicastIPAddressInformation in allNetworkInterfaces[i].GetIPProperties().UnicastAddresses)
					{
						if (unicastIPAddressInformation.Address.AddressFamily != AddressFamily.InterNetwork)
						{
							AddressFamily addressFamily = unicastIPAddressInformation.Address.AddressFamily;
						}
					}
				}
			}
			catch (Exception)
			{
			}
		}

		internal static int RelativeSequenceNumber(int number, int expected)
		{
			return (number - expected + 32768 + 16384) % 32768 - 16384;
		}

		internal static T[] AllocatePinnedUninitializedArray<[IsUnmanaged] T>(int count) where T : struct, ValueType
		{
			return new T[count];
		}

		private static readonly List<string> IpList = new List<string>();
	}
}
