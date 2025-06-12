using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LiteNetLib4Mirror.Open.Nat;

public abstract class NatDevice
{
	private readonly HashSet<Mapping> _openedMapping = new HashSet<Mapping>();

	protected DateTime LastSeen { get; private set; }

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
		int num = mappings.ToArray().Length;
		NatDiscoverer.TraceSource.LogInfo("{0} ports to close", num);
		for (int i = 0; i < num; i++)
		{
			Mapping mapping = this._openedMapping.ElementAt(i);
			try
			{
				this.DeletePortMapAsync(mapping);
				NatDiscoverer.TraceSource.LogInfo(mapping?.ToString() + " port successfully closed");
			}
			catch (Exception)
			{
				NatDiscoverer.TraceSource.LogError(mapping?.ToString() + " port couldn't be close");
			}
		}
	}

	internal void ReleaseAll()
	{
		this.ReleaseMapping(this._openedMapping);
	}

	internal void ReleaseSessionMappings()
	{
		IEnumerable<Mapping> mappings = this._openedMapping.Where((Mapping m) => m.LifetimeType == MappingLifetime.Session);
		this.ReleaseMapping(mappings);
	}

	internal async Task RenewMappings()
	{
		IEnumerable<Mapping> source = this._openedMapping.Where((Mapping x) => x.ShoundRenew());
		Mapping[] array = source.ToArray();
		foreach (Mapping mapping in array)
		{
			await this.RenewMapping(mapping);
		}
	}

	private async Task RenewMapping(Mapping mapping)
	{
		Mapping renewMapping = new Mapping(mapping);
		try
		{
			renewMapping.Expiration = DateTime.UtcNow.AddSeconds(mapping.Lifetime);
			NatDiscoverer.TraceSource.LogInfo("Renewing mapping {0}", renewMapping);
			await this.CreatePortMapAsync(renewMapping);
			NatDiscoverer.TraceSource.LogInfo("Next renew scheduled at: {0}", renewMapping.Expiration.ToLocalTime().TimeOfDay);
		}
		catch (Exception)
		{
			NatDiscoverer.TraceSource.LogWarn("Renew {0} failed", mapping);
		}
	}
}
