using System;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal sealed class Finalizer
	{
		~Finalizer()
		{
			NatDiscoverer.TraceSource.LogInfo("Closing ports opened in this session", Array.Empty<object>());
			NatDiscoverer.RenewTimer.Dispose();
			NatDiscoverer.ReleaseSessionMappings();
		}
	}
}
