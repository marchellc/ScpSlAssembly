using System;
using Mirror;

namespace InventorySystem.Searching;

public struct SearchInvalidation : ISearchIdentifiable, NetworkMessage, IEquatable<SearchInvalidation>
{
	public byte Id { get; private set; }

	public SearchInvalidation(byte id)
	{
		this.Id = id;
	}

	public void Deserialize(NetworkReader reader)
	{
		this.Id = reader.ReadByte();
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteByte(this.Id);
	}

	public bool Equals(SearchInvalidation other)
	{
		return this.Id == other.Id;
	}

	public override bool Equals(object obj)
	{
		if (obj is SearchInvalidation other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return this.Id.GetHashCode();
	}
}
