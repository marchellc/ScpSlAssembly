using System;

namespace CentralAuth
{
	public enum ClientInstanceMode : byte
	{
		Unverified,
		ReadyClient,
		Host,
		DedicatedServer,
		Dummy
	}
}
