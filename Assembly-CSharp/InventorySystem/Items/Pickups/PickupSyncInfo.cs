using System;

namespace InventorySystem.Items.Pickups;

[Serializable]
public struct PickupSyncInfo : IEquatable<PickupSyncInfo>
{
	[Flags]
	private enum PickupFlags : byte
	{
		Locked = 1,
		InUse = 2
	}

	public static PickupSyncInfo None = new PickupSyncInfo
	{
		ItemId = ItemType.None
	};

	public ItemType ItemId;

	public ushort Serial;

	public float WeightKg;

	private PickupFlags _flags;

	public byte SyncedFlags
	{
		get
		{
			return (byte)this._flags;
		}
		set
		{
			this._flags = (PickupFlags)value;
		}
	}

	public bool Locked
	{
		get
		{
			return (this._flags & PickupFlags.Locked) == PickupFlags.Locked;
		}
		set
		{
			this._flags = (value ? (this._flags | PickupFlags.Locked) : (this._flags & ~PickupFlags.Locked));
		}
	}

	public bool InUse
	{
		get
		{
			return (this._flags & PickupFlags.InUse) == PickupFlags.InUse;
		}
		set
		{
			this._flags = (value ? (this._flags | PickupFlags.InUse) : (this._flags & ~PickupFlags.InUse));
		}
	}

	public PickupSyncInfo(ItemType id, float weight, ushort serial = 0, bool locked = false)
	{
		this.ItemId = id;
		this.WeightKg = weight;
		this._flags = (locked ? PickupFlags.Locked : ((PickupFlags)0));
		this.Serial = ((serial == 0) ? ItemSerialGenerator.GenerateNext() : serial);
	}

	public override int GetHashCode()
	{
		if (this.Serial == 0)
		{
			return ((int)this.ItemId * 397) ^ (int)this._flags;
		}
		return this.Serial;
	}

	public static bool operator ==(PickupSyncInfo left, PickupSyncInfo right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(PickupSyncInfo left, PickupSyncInfo right)
	{
		return !left.Equals(right);
	}

	public bool Equals(PickupSyncInfo other)
	{
		if (this.ItemId == other.ItemId && this.WeightKg == other.WeightKg)
		{
			return this._flags == other._flags;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is PickupSyncInfo other)
		{
			return this.Equals(other);
		}
		return false;
	}
}
