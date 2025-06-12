using System;
using System.Collections.Generic;

public struct RecyclablePlayerId : IEquatable<RecyclablePlayerId>
{
	private const int MinQueue = 16;

	private static readonly Queue<int> FreeIds = new Queue<int>();

	private static int _autoIncrement;

	public readonly int Value;

	public RecyclablePlayerId(int newId)
	{
		this.Value = newId;
	}

	public RecyclablePlayerId(ReferenceHub hub)
	{
		this.Value = hub.PlayerId;
	}

	public RecyclablePlayerId(bool useMinQueue)
	{
		int num = (useMinQueue ? 16 : 0);
		int value = ((RecyclablePlayerId.FreeIds.Count >= num) ? RecyclablePlayerId.FreeIds.Dequeue() : (++RecyclablePlayerId._autoIncrement));
		this.Value = value;
	}

	public void Destroy()
	{
		if (this.Value != 0)
		{
			RecyclablePlayerId.FreeIds.Enqueue(this.Value);
		}
	}

	public bool Equals(RecyclablePlayerId other)
	{
		return this.Value == other.Value;
	}

	public override bool Equals(object obj)
	{
		if (obj is RecyclablePlayerId other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return this.Value;
	}

	public static bool operator ==(RecyclablePlayerId left, RecyclablePlayerId right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RecyclablePlayerId left, RecyclablePlayerId right)
	{
		return !left.Equals(right);
	}
}
