using Decals;
using Mirror;

namespace CommandSystem.Commands.RemoteAdmin.Cleanup;

public readonly struct DecalCleanupMessage : NetworkMessage
{
	public readonly DecalPoolType DecalPoolType;

	public readonly int Amount;

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteByte((byte)this.DecalPoolType);
		writer.WriteInt(this.Amount);
	}

	public DecalCleanupMessage(NetworkReader reader)
	{
		this.DecalPoolType = (DecalPoolType)reader.ReadByte();
		this.Amount = reader.ReadInt();
	}

	public DecalCleanupMessage(DecalPoolType decalPoolType, int amount)
	{
		this.DecalPoolType = decalPoolType;
		this.Amount = amount;
	}
}
