using System;
using Mirror;

namespace RoundRestarting
{
	public readonly struct RoundRestartMessage : NetworkMessage
	{
		public RoundRestartMessage(RoundRestartType type, float offset, ushort newport, bool reconnect, bool extendedReconnectionPeriod)
		{
			this.Type = type;
			this.TimeOffset = offset;
			this.NewPort = newport;
			this.Reconnect = reconnect;
			this.ExtendedReconnectionPeriod = extendedReconnectionPeriod;
		}

		public readonly RoundRestartType Type;

		public readonly float TimeOffset;

		public readonly ushort NewPort;

		public readonly bool Reconnect;

		public readonly bool ExtendedReconnectionPeriod;
	}
}
