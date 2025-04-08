using System;
using Mirror;

public struct TeslaHitMsg : NetworkMessage
{
	public TeslaHitMsg(TeslaGate gate)
	{
		this.Gate = gate;
	}

	public readonly TeslaGate Gate;
}
