using System;

namespace LiteNetLib
{
	[Flags]
	public enum ConnectionState : byte
	{
		Outgoing = 2,
		Connected = 4,
		ShutdownRequested = 8,
		Disconnected = 16,
		EndPointChange = 32,
		Any = 46
	}
}
