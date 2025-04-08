using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteNetLib4Mirror.Open.Nat
{
	public class NatDiscoverer
	{
		public async Task<NatDevice> DiscoverDeviceAsync()
		{
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(3000);
			return await this.DiscoverDeviceAsync(PortMapper.Pmp | PortMapper.Upnp, cancellationTokenSource);
		}

		public async Task<NatDevice> DiscoverDeviceAsync(PortMapper portMapper, CancellationTokenSource cancellationTokenSource)
		{
			Guard.IsTrue(portMapper.HasFlag(PortMapper.Upnp) || portMapper.HasFlag(PortMapper.Pmp), "portMapper");
			Guard.IsNotNull(cancellationTokenSource, "cancellationTokenSource");
			NatDevice natDevice = (await this.DiscoverAsync(portMapper, true, cancellationTokenSource)).FirstOrDefault<NatDevice>();
			if (natDevice == null)
			{
				NatDiscoverer.TraceSource.LogInfo("Device not found. Common reasons:", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogInfo("\t* No device is present or,", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogInfo("\t* Upnp is disabled in the router or", Array.Empty<object>());
				NatDiscoverer.TraceSource.LogInfo("\t* Antivirus software is filtering SSDP (discovery protocol).", Array.Empty<object>());
				throw new NatDeviceNotFoundException();
			}
			return natDevice;
		}

		public async Task<IEnumerable<NatDevice>> DiscoverDevicesAsync(PortMapper portMapper, CancellationTokenSource cancellationTokenSource)
		{
			Guard.IsTrue(portMapper.HasFlag(PortMapper.Upnp) || portMapper.HasFlag(PortMapper.Pmp), "portMapper");
			Guard.IsNotNull(cancellationTokenSource, "cancellationTokenSource");
			return (await this.DiscoverAsync(portMapper, false, cancellationTokenSource)).ToArray<NatDevice>();
		}

		private async Task<IEnumerable<NatDevice>> DiscoverAsync(PortMapper portMapper, bool onlyOne, CancellationTokenSource cts)
		{
			NatDiscoverer.TraceSource.LogInfo("Start Discovery", Array.Empty<object>());
			List<Task<IEnumerable<NatDevice>>> searcherTasks = new List<Task<IEnumerable<NatDevice>>>();
			if (portMapper.HasFlag(PortMapper.Upnp))
			{
				UpnpSearcher upnpSearcher = new UpnpSearcher(new IPAddressesProvider());
				UpnpSearcher upnpSearcher2 = upnpSearcher;
				upnpSearcher2.DeviceFound = (EventHandler<DeviceEventArgs>)Delegate.Combine(upnpSearcher2.DeviceFound, new EventHandler<DeviceEventArgs>(delegate(object sender, DeviceEventArgs args)
				{
					if (onlyOne)
					{
						cts.Cancel();
					}
				}));
				searcherTasks.Add(upnpSearcher.Search(cts.Token));
			}
			if (portMapper.HasFlag(PortMapper.Pmp))
			{
				PmpSearcher pmpSearcher = new PmpSearcher(new IPAddressesProvider());
				PmpSearcher pmpSearcher2 = pmpSearcher;
				pmpSearcher2.DeviceFound = (EventHandler<DeviceEventArgs>)Delegate.Combine(pmpSearcher2.DeviceFound, new EventHandler<DeviceEventArgs>(delegate(object sender, DeviceEventArgs args)
				{
					if (onlyOne)
					{
						cts.Cancel();
					}
				}));
				searcherTasks.Add(pmpSearcher.Search(cts.Token));
			}
			await Task.WhenAll<IEnumerable<NatDevice>>(searcherTasks);
			NatDiscoverer.TraceSource.LogInfo("Stop Discovery", Array.Empty<object>());
			IEnumerable<NatDevice> enumerable = searcherTasks.SelectMany((Task<IEnumerable<NatDevice>> x) => x.Result);
			foreach (NatDevice natDevice in enumerable)
			{
				string text = natDevice.ToString();
				NatDevice natDevice2;
				if (NatDiscoverer.Devices.TryGetValue(text, out natDevice2))
				{
					natDevice2.Touch();
				}
				else
				{
					NatDiscoverer.Devices.Add(text, natDevice);
				}
			}
			return enumerable;
		}

		public static void ReleaseAll()
		{
			foreach (NatDevice natDevice in NatDiscoverer.Devices.Values)
			{
				natDevice.ReleaseAll();
			}
		}

		internal static void ReleaseSessionMappings()
		{
			foreach (NatDevice natDevice in NatDiscoverer.Devices.Values)
			{
				natDevice.ReleaseSessionMappings();
			}
		}

		private static void RenewMappings(object state)
		{
			Task.Factory.StartNew<Task>(async delegate
			{
				foreach (NatDevice natDevice in NatDiscoverer.Devices.Values)
				{
					await natDevice.RenewMappings();
				}
				Dictionary<string, NatDevice>.ValueCollection.Enumerator enumerator = default(Dictionary<string, NatDevice>.ValueCollection.Enumerator);
			});
		}

		public static readonly TraceSource TraceSource = new TraceSource("Open.NAT");

		private static readonly Dictionary<string, NatDevice> Devices = new Dictionary<string, NatDevice>();

		private static readonly Finalizer Finalizer = new Finalizer();

		internal static readonly Timer RenewTimer = new Timer(new TimerCallback(NatDiscoverer.RenewMappings), null, 5000, 2000);
	}
}
