using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class UpnpSearcher : Searcher
	{
		internal UpnpSearcher(IIPAddressesProvider ipprovider)
		{
			this._ipprovider = ipprovider;
			this.UdpClients = this.CreateUdpClients();
			this._devices = new Dictionary<Uri, NatDevice>();
			this._lastFetched = new Dictionary<IPAddress, DateTime>();
		}

		private List<UdpClient> CreateUdpClients()
		{
			List<UdpClient> list = new List<UdpClient>();
			try
			{
				foreach (IPAddress ipaddress in this._ipprovider.UnicastAddresses())
				{
					try
					{
						list.Add(new UdpClient(new IPEndPoint(ipaddress, 0)));
					}
					catch (Exception)
					{
					}
				}
			}
			catch (Exception)
			{
				list.Add(new UdpClient(0));
			}
			return list;
		}

		protected override void Discover(UdpClient client, CancellationToken cancelationToken)
		{
			this.Discover(client, WellKnownConstants.IPv4MulticastAddress, cancelationToken);
			if (Socket.OSSupportsIPv6)
			{
				this.Discover(client, WellKnownConstants.IPv6LinkLocalMulticastAddress, cancelationToken);
				this.Discover(client, WellKnownConstants.IPv6LinkSiteMulticastAddress, cancelationToken);
			}
		}

		private void Discover(UdpClient client, IPAddress address, CancellationToken cancelationToken)
		{
			if (!this.IsValidClient(client.Client, address))
			{
				return;
			}
			this.NextSearch = DateTime.UtcNow.AddSeconds(1.0);
			IPEndPoint ipendPoint = new IPEndPoint(address, 1900);
			string[] serviceTypes = UpnpSearcher.ServiceTypes;
			for (int i = 0; i < serviceTypes.Length; i++)
			{
				string text = DiscoverDeviceMessage.Encode(serviceTypes[i], address);
				byte[] bytes = Encoding.ASCII.GetBytes(text);
				for (int j = 0; j < 3; j++)
				{
					if (cancelationToken.IsCancellationRequested)
					{
						return;
					}
					client.Send(bytes, bytes.Length, ipendPoint);
				}
			}
		}

		private bool IsValidClient(Socket socket, IPAddress address)
		{
			IPEndPoint ipendPoint = (IPEndPoint)socket.LocalEndPoint;
			if (socket.AddressFamily != address.AddressFamily)
			{
				return false;
			}
			AddressFamily addressFamily = socket.AddressFamily;
			if (addressFamily == AddressFamily.InterNetwork)
			{
				socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastInterface, ipendPoint.Address.GetAddressBytes());
				return true;
			}
			if (addressFamily != AddressFamily.InterNetworkV6)
			{
				return false;
			}
			if (ipendPoint.Address.IsIPv6LinkLocal && !object.Equals(address, WellKnownConstants.IPv6LinkLocalMulticastAddress))
			{
				return false;
			}
			if (!ipendPoint.Address.IsIPv6LinkLocal && !object.Equals(address, WellKnownConstants.IPv6LinkSiteMulticastAddress))
			{
				return false;
			}
			socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, BitConverter.GetBytes((int)ipendPoint.Address.ScopeId));
			return true;
		}

		public override NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint)
		{
			string text = null;
			try
			{
				text = Encoding.UTF8.GetString(response);
				DiscoveryResponseMessage discoveryResponseMessage = new DiscoveryResponseMessage(text);
				string text2 = discoveryResponseMessage["ST"];
				if (!UpnpSearcher.IsValidControllerService(text2))
				{
					NatDiscoverer.TraceSource.LogWarn("Invalid controller service. Ignoring.", Array.Empty<object>());
					return null;
				}
				NatDiscoverer.TraceSource.LogInfo("UPnP Response: Router advertised a '{0}' service!!!", new object[] { text2 });
				Uri uri = new Uri(discoveryResponseMessage["Location"] ?? discoveryResponseMessage["AL"]);
				NatDiscoverer.TraceSource.LogInfo("Found device at: {0}", new object[] { uri.ToString() });
				if (this._devices.ContainsKey(uri))
				{
					NatDiscoverer.TraceSource.LogInfo("Already found - Ignored", Array.Empty<object>());
					this._devices[uri].Touch();
					return null;
				}
				if (this._lastFetched.ContainsKey(endpoint.Address))
				{
					DateTime dateTime = this._lastFetched[endpoint.Address];
					if (DateTime.Now - dateTime < TimeSpan.FromSeconds(20.0))
					{
						return null;
					}
				}
				this._lastFetched[endpoint.Address] = DateTime.Now;
				NatDiscoverer.TraceSource.LogInfo("{0}:{1}: Fetching service list", new object[] { uri.Host, uri.Port });
				UpnpNatDeviceInfo upnpNatDeviceInfo = this.BuildUpnpNatDeviceInfo(localAddress, uri);
				IDictionary<Uri, NatDevice> devices = this._devices;
				UpnpNatDevice upnpNatDevice;
				lock (devices)
				{
					upnpNatDevice = new UpnpNatDevice(upnpNatDeviceInfo);
					if (!this._devices.ContainsKey(uri))
					{
						this._devices.Add(uri, upnpNatDevice);
					}
				}
				return upnpNatDevice;
			}
			catch (Exception ex)
			{
				NatDiscoverer.TraceSource.LogError("Unhandled exception when trying to decode a device's response. ", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError("Report the issue in https://github.com/lontivero/LiteNetLib4Mirror.Open.Nat/issues", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError("Also copy and paste the following info:", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError("-- beging ---------------------------------", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError(ex.Message, Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError("Data string:", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError(text ?? "No data available", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogError("-- end ------------------------------------", Array.Empty<object>());
			}
			return null;
		}

		private static bool IsValidControllerService(string serviceType)
		{
			return (from serviceName in UpnpSearcher.ServiceTypes
				let serviceUrn = "urn:schemas-upnp-org:service:" + serviceName
				where serviceType.ContainsIgnoreCase(serviceUrn)
				select new
				{
					ServiceName = serviceName,
					ServiceUrn = serviceUrn
				}).Any();
		}

		private UpnpNatDeviceInfo BuildUpnpNatDeviceInfo(IPAddress localAddress, Uri location)
		{
			NatDiscoverer.TraceSource.LogInfo("Found device at: {0}", new object[] { location.ToString() });
			IPEndPoint ipendPoint = new IPEndPoint(IPAddress.Parse(location.Host), location.Port);
			WebResponse webResponse = null;
			try
			{
				HttpWebRequest httpWebRequest = WebRequest.CreateHttp(location);
				httpWebRequest.Headers.Add("ACCEPT-LANGUAGE", "en");
				httpWebRequest.Method = "GET";
				webResponse = httpWebRequest.GetResponse();
				HttpWebResponse httpWebResponse = webResponse as HttpWebResponse;
				if (httpWebResponse != null && httpWebResponse.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception(string.Format("Couldn't get services list: {0} {1}", httpWebResponse.StatusCode, httpWebResponse.StatusDescription));
				}
				XmlDocument xmlDocument = UpnpSearcher.ReadXmlResponse(webResponse);
				NatDiscoverer.TraceSource.LogInfo("{0}: Parsed services list", new object[] { ipendPoint });
				XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
				xmlNamespaceManager.AddNamespace("ns", "urn:schemas-upnp-org:device-1-0");
				foreach (object obj in xmlDocument.SelectNodes("//ns:service", xmlNamespaceManager))
				{
					XmlNode xmlNode = (XmlNode)obj;
					string xmlElementText = xmlNode.GetXmlElementText("serviceType");
					if (UpnpSearcher.IsValidControllerService(xmlElementText))
					{
						NatDiscoverer.TraceSource.LogInfo("{0}: Found service: {1}", new object[] { ipendPoint, xmlElementText });
						string xmlElementText2 = xmlNode.GetXmlElementText("controlURL");
						NatDiscoverer.TraceSource.LogInfo("{0}: Found upnp service at: {1}", new object[] { ipendPoint, xmlElementText2 });
						NatDiscoverer.TraceSource.LogInfo("{0}: Handshake Complete", new object[] { ipendPoint });
						return new UpnpNatDeviceInfo(localAddress, location, xmlElementText2, xmlElementText);
					}
				}
				throw new Exception("No valid control service was found in the service descriptor document");
			}
			catch (WebException ex)
			{
				NatDiscoverer.TraceSource.LogError("{0}: Device denied the connection attempt: {1}", new object[] { ipendPoint, ex });
				SocketException ex2 = ex.InnerException as SocketException;
				if (ex2 != null)
				{
					NatDiscoverer.TraceSource.LogError("{0}: ErrorCode:{1}", new object[] { ipendPoint, ex2.ErrorCode });
					NatDiscoverer.TraceSource.LogError("Go to http://msdn.microsoft.com/en-us/library/system.net.sockets.socketerror.aspx", Array.Empty<object>());
					NatDiscoverer.TraceSource.LogError("Usually this happens. Try resetting the device and try again. If you are in a VPN, disconnect and try again.", Array.Empty<object>());
				}
				throw;
			}
			finally
			{
				if (webResponse != null)
				{
					webResponse.Close();
				}
			}
			UpnpNatDeviceInfo upnpNatDeviceInfo;
			return upnpNatDeviceInfo;
		}

		private static XmlDocument ReadXmlResponse(WebResponse response)
		{
			XmlDocument xmlDocument2;
			using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
			{
				string text = streamReader.ReadToEnd();
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(text);
				xmlDocument2 = xmlDocument;
			}
			return xmlDocument2;
		}

		private readonly IIPAddressesProvider _ipprovider;

		private readonly IDictionary<Uri, NatDevice> _devices;

		private readonly Dictionary<IPAddress, DateTime> _lastFetched;

		private static readonly string[] ServiceTypes = new string[] { "WANIPConnection:2", "WANPPPConnection:2", "WANIPConnection:1", "WANPPPConnection:1" };
	}
}
