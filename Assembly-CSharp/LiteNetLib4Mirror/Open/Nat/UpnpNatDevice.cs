using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal sealed class UpnpNatDevice : NatDevice
	{
		internal UpnpNatDevice(UpnpNatDeviceInfo deviceInfo)
		{
			base.Touch();
			this.DeviceInfo = deviceInfo;
			this._soapClient = new SoapClient(this.DeviceInfo.ServiceControlUri, this.DeviceInfo.ServiceType);
		}

		public override async Task<IPAddress> GetExternalIPAsync()
		{
			NatDiscoverer.TraceSource.LogInfo("GetExternalIPAsync - Getting external IP address", Array.Empty<object>());
			GetExternalIPAddressRequestMessage getExternalIPAddressRequestMessage = new GetExternalIPAddressRequestMessage();
			TaskAwaiter<XmlDocument> taskAwaiter = this._soapClient.InvokeAsync("GetExternalIPAddress", getExternalIPAddressRequestMessage.ToXml()).TimeoutAfter(TimeSpan.FromSeconds(4.0)).GetAwaiter();
			if (!taskAwaiter.IsCompleted)
			{
				await taskAwaiter;
				TaskAwaiter<XmlDocument> taskAwaiter2;
				taskAwaiter = taskAwaiter2;
				taskAwaiter2 = default(TaskAwaiter<XmlDocument>);
			}
			return new GetExternalIPAddressResponseMessage(taskAwaiter.GetResult(), this.DeviceInfo.ServiceType).ExternalIPAddress;
		}

		public override async Task CreatePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, "mapping");
			if (mapping.PrivateIP.Equals(IPAddress.None))
			{
				mapping.PrivateIP = this.DeviceInfo.LocalAddress;
			}
			NatDiscoverer.TraceSource.LogInfo("CreatePortMapAsync - Creating port mapping {0}", new object[] { mapping });
			bool retry = false;
			try
			{
				CreatePortMappingRequestMessage createPortMappingRequestMessage = new CreatePortMappingRequestMessage(mapping);
				await this._soapClient.InvokeAsync("AddPortMapping", createPortMappingRequestMessage.ToXml()).TimeoutAfter(TimeSpan.FromSeconds(4.0));
				base.RegisterMapping(mapping);
			}
			catch (MappingException ex)
			{
				int errorCode = ex.ErrorCode;
				if (errorCode == 718)
				{
					NatDiscoverer.TraceSource.LogWarn("Conflict with an already existing mapping", Array.Empty<object>());
					throw;
				}
				switch (errorCode)
				{
				case 724:
					NatDiscoverer.TraceSource.LogWarn("Same Port Values Required - Using internal port {0}", new object[] { mapping.PrivatePort });
					mapping.PublicPort = mapping.PrivatePort;
					retry = true;
					break;
				case 725:
					NatDiscoverer.TraceSource.LogWarn("Only Permanent Leases Supported - There is no warranty it will be closed", Array.Empty<object>());
					mapping.Lifetime = 0;
					mapping.LifetimeType = MappingLifetime.ForcedSession;
					retry = true;
					break;
				case 726:
					NatDiscoverer.TraceSource.LogWarn("Remote Host Only Supports Wildcard", Array.Empty<object>());
					mapping.PublicIP = IPAddress.None;
					retry = true;
					break;
				case 727:
					NatDiscoverer.TraceSource.LogWarn("External Port Only Supports Wildcard", Array.Empty<object>());
					throw;
				default:
					throw;
				}
			}
			if (retry)
			{
				await this.CreatePortMapAsync(mapping);
			}
		}

		public override async Task DeletePortMapAsync(Mapping mapping)
		{
			Guard.IsNotNull(mapping, "mapping");
			if (mapping.PrivateIP.Equals(IPAddress.None))
			{
				mapping.PrivateIP = this.DeviceInfo.LocalAddress;
			}
			NatDiscoverer.TraceSource.LogInfo("DeletePortMapAsync - Deleteing port mapping {0}", new object[] { mapping });
			try
			{
				DeletePortMappingRequestMessage deletePortMappingRequestMessage = new DeletePortMappingRequestMessage(mapping);
				await this._soapClient.InvokeAsync("DeletePortMapping", deletePortMappingRequestMessage.ToXml()).TimeoutAfter(TimeSpan.FromSeconds(4.0));
				base.UnregisterMapping(mapping);
			}
			catch (MappingException ex)
			{
				if (ex.ErrorCode != 714)
				{
					throw;
				}
			}
		}

		public override async Task<IEnumerable<Mapping>> GetAllMappingsAsync()
		{
			int index = 0;
			List<Mapping> mappings = new List<Mapping>();
			NatDiscoverer.TraceSource.LogInfo("GetAllMappingsAsync - Getting all mappings", Array.Empty<object>());
			for (;;)
			{
				try
				{
					int num = index;
					index = num + 1;
					GetGenericPortMappingEntry getGenericPortMappingEntry = new GetGenericPortMappingEntry(num);
					TaskAwaiter<XmlDocument> taskAwaiter = this._soapClient.InvokeAsync("GetGenericPortMappingEntry", getGenericPortMappingEntry.ToXml()).TimeoutAfter(TimeSpan.FromSeconds(4.0)).GetAwaiter();
					if (!taskAwaiter.IsCompleted)
					{
						await taskAwaiter;
						TaskAwaiter<XmlDocument> taskAwaiter2;
						taskAwaiter = taskAwaiter2;
						taskAwaiter2 = default(TaskAwaiter<XmlDocument>);
					}
					GetPortMappingEntryResponseMessage getPortMappingEntryResponseMessage = new GetPortMappingEntryResponseMessage(taskAwaiter.GetResult(), this.DeviceInfo.ServiceType, true);
					IPAddress ipaddress;
					if (!IPAddress.TryParse(getPortMappingEntryResponseMessage.InternalClient, out ipaddress))
					{
						NatDiscoverer.TraceSource.LogWarn("InternalClient is not an IP address. Mapping ignored!", Array.Empty<object>());
						continue;
					}
					Mapping mapping = new Mapping(getPortMappingEntryResponseMessage.NetworkProtocolType, ipaddress, getPortMappingEntryResponseMessage.InternalPort, getPortMappingEntryResponseMessage.ExternalPort, getPortMappingEntryResponseMessage.LeaseDuration, getPortMappingEntryResponseMessage.PortMappingDescription);
					mappings.Add(mapping);
					continue;
				}
				catch (MappingException ex)
				{
					if (ex.ErrorCode != 713 && ex.ErrorCode != 714 && ex.ErrorCode != 402 && ex.ErrorCode != 501)
					{
						throw;
					}
					NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", new object[] { ex.ErrorCode, ex.ErrorText });
				}
				break;
			}
			return mappings.ToArray();
		}

		public override async Task<Mapping> GetSpecificMappingAsync(NetworkProtocolType networkProtocolType, int publicPort)
		{
			Guard.IsTrue(networkProtocolType == NetworkProtocolType.Tcp || networkProtocolType == NetworkProtocolType.Udp, "protocol");
			Guard.IsInRange(publicPort, 0, 65535, "port");
			NatDiscoverer.TraceSource.LogInfo("GetSpecificMappingAsync - Getting mapping for protocol: {0} port: {1}", new object[]
			{
				Enum.GetName(typeof(NetworkProtocolType), networkProtocolType),
				publicPort
			});
			Mapping mapping;
			try
			{
				GetSpecificPortMappingEntryRequestMessage getSpecificPortMappingEntryRequestMessage = new GetSpecificPortMappingEntryRequestMessage(networkProtocolType, publicPort);
				TaskAwaiter<XmlDocument> taskAwaiter = this._soapClient.InvokeAsync("GetSpecificPortMappingEntry", getSpecificPortMappingEntryRequestMessage.ToXml()).TimeoutAfter(TimeSpan.FromSeconds(4.0)).GetAwaiter();
				if (!taskAwaiter.IsCompleted)
				{
					await taskAwaiter;
					TaskAwaiter<XmlDocument> taskAwaiter2;
					taskAwaiter = taskAwaiter2;
					taskAwaiter2 = default(TaskAwaiter<XmlDocument>);
				}
				GetPortMappingEntryResponseMessage getPortMappingEntryResponseMessage = new GetPortMappingEntryResponseMessage(taskAwaiter.GetResult(), this.DeviceInfo.ServiceType, false);
				if (getPortMappingEntryResponseMessage.NetworkProtocolType != networkProtocolType)
				{
					NatDiscoverer.TraceSource.LogWarn("Router responded to a protocol {0} query with a protocol {1} answer, work around applied.", new object[] { networkProtocolType, getPortMappingEntryResponseMessage.NetworkProtocolType });
				}
				mapping = new Mapping(networkProtocolType, IPAddress.Parse(getPortMappingEntryResponseMessage.InternalClient), getPortMappingEntryResponseMessage.InternalPort, publicPort, getPortMappingEntryResponseMessage.LeaseDuration, getPortMappingEntryResponseMessage.PortMappingDescription);
			}
			catch (MappingException ex)
			{
				if (ex.ErrorCode != 713 && ex.ErrorCode != 714 && ex.ErrorCode != 402 && ex.ErrorCode != 501)
				{
					throw;
				}
				NatDiscoverer.TraceSource.LogWarn("Router failed with {0}-{1}. No more mappings is assumed.", new object[] { ex.ErrorCode, ex.ErrorText });
				mapping = null;
			}
			return mapping;
		}

		public override string ToString()
		{
			return string.Format("EndPoint: {0}\nControl Url: {1}\nService Type: {2}\nLast Seen: {3}", new object[]
			{
				this.DeviceInfo.HostEndPoint,
				this.DeviceInfo.ServiceControlUri,
				this.DeviceInfo.ServiceType,
				base.LastSeen
			});
		}

		internal readonly UpnpNatDeviceInfo DeviceInfo;

		private readonly SoapClient _soapClient;
	}
}
