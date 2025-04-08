using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LiteNetLib4Mirror.Open.Nat
{
	public abstract class NatDevice
	{
		private protected DateTime LastSeen { protected get; private set; }

		internal void Touch()
		{
			this.LastSeen = DateTime.Now;
		}

		public abstract Task CreatePortMapAsync(Mapping mapping);

		public abstract Task DeletePortMapAsync(Mapping mapping);

		public abstract Task<IEnumerable<Mapping>> GetAllMappingsAsync();

		public abstract Task<IPAddress> GetExternalIPAsync();

		public abstract Task<Mapping> GetSpecificMappingAsync(NetworkProtocolType networkProtocolType, int port);

		protected void RegisterMapping(Mapping mapping)
		{
			this._openedMapping.Remove(mapping);
			this._openedMapping.Add(mapping);
		}

		protected void UnregisterMapping(Mapping mapping)
		{
			this._openedMapping.RemoveWhere((Mapping x) => x.Equals(mapping));
		}

		internal void ReleaseMapping(IEnumerable<Mapping> mappings)
		{
			int num = mappings.ToArray<Mapping>().Length;
			NatDiscoverer.TraceSource.LogInfo("{0} ports to close", new object[] { num });
			for (int i = 0; i < num; i++)
			{
				Mapping mapping = this._openedMapping.ElementAt(i);
				try
				{
					this.DeletePortMapAsync(mapping);
					TraceSource traceSource = NatDiscoverer.TraceSource;
					Mapping mapping2 = mapping;
					traceSource.LogInfo(((mapping2 != null) ? mapping2.ToString() : null) + " port successfully closed", Array.Empty<object>());
				}
				catch (Exception)
				{
					TraceSource traceSource2 = NatDiscoverer.TraceSource;
					Mapping mapping3 = mapping;
					traceSource2.LogError(((mapping3 != null) ? mapping3.ToString() : null) + " port couldn't be close", Array.Empty<object>());
				}
			}
		}

		internal void ReleaseAll()
		{
			this.ReleaseMapping(this._openedMapping);
		}

		internal void ReleaseSessionMappings()
		{
			IEnumerable<Mapping> enumerable = this._openedMapping.Where((Mapping m) => m.LifetimeType == MappingLifetime.Session);
			this.ReleaseMapping(enumerable);
		}

		internal async Task RenewMappings()
		{
			IEnumerable<Mapping> enumerable = this._openedMapping.Where((Mapping x) => x.ShoundRenew());
			foreach (Mapping mapping in enumerable.ToArray<Mapping>())
			{
				await this.RenewMapping(mapping);
			}
			Mapping[] array = null;
		}

		private async Task RenewMapping(Mapping mapping)
		{
			Mapping renewMapping = new Mapping(mapping);
			try
			{
				renewMapping.Expiration = DateTime.UtcNow.AddSeconds((double)mapping.Lifetime);
				NatDiscoverer.TraceSource.LogInfo("Renewing mapping {0}", new object[] { renewMapping });
				await this.CreatePortMapAsync(renewMapping);
				NatDiscoverer.TraceSource.LogInfo("Next renew scheduled at: {0}", new object[] { renewMapping.Expiration.ToLocalTime().TimeOfDay });
			}
			catch (Exception)
			{
				NatDiscoverer.TraceSource.LogWarn("Renew {0} failed", new object[] { mapping });
			}
		}

		private readonly HashSet<Mapping> _openedMapping = new HashSet<Mapping>();
	}
}
