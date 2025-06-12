using Mirror;

namespace InventorySystem.Disarming;

public readonly struct DisarmMessage : NetworkMessage
{
	public readonly ReferenceHub PlayerToDisarm;

	public readonly bool Disarm;

	public readonly bool PlayerIsNull;

	public DisarmMessage(ReferenceHub playerToDisarm, bool disarm, bool isNull)
	{
		this.PlayerToDisarm = playerToDisarm;
		this.Disarm = disarm;
		this.PlayerIsNull = isNull;
	}
}
