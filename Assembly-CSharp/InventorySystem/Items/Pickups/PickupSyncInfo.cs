using System;

namespace InventorySystem.Items.Pickups
{
	[Serializable]
	public struct PickupSyncInfo : IEquatable<PickupSyncInfo>
	{
		public PickupSyncInfo(ItemType id, float weight, ushort serial = 0, bool locked = false)
		{
			this.ItemId = id;
			this.WeightKg = weight;
			this._flags = (locked ? PickupSyncInfo.PickupFlags.Locked : ((PickupSyncInfo.PickupFlags)0));
			this.Serial = ((serial == 0) ? ItemSerialGenerator.GenerateNext() : serial);
		}

		public byte SyncedFlags
		{
			get
			{
				return (byte)this._flags;
			}
			set
			{
				this._flags = (PickupSyncInfo.PickupFlags)value;
			}
		}

		public bool Locked
		{
			get
			{
				return (this._flags & PickupSyncInfo.PickupFlags.Locked) == PickupSyncInfo.PickupFlags.Locked;
			}
			set
			{
				this._flags = (value ? (this._flags | PickupSyncInfo.PickupFlags.Locked) : (this._flags & ~PickupSyncInfo.PickupFlags.Locked));
			}
		}

		public bool InUse
		{
			get
			{
				return (this._flags & PickupSyncInfo.PickupFlags.InUse) == PickupSyncInfo.PickupFlags.InUse;
			}
			set
			{
				this._flags = (value ? (this._flags | PickupSyncInfo.PickupFlags.InUse) : (this._flags & ~PickupSyncInfo.PickupFlags.InUse));
			}
		}

		public override int GetHashCode()
		{
			if (this.Serial == 0)
			{
				return (int)((this.ItemId * (ItemType)397) ^ (ItemType)this._flags);
			}
			return (int)this.Serial;
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
			return this.ItemId == other.ItemId && this.WeightKg == other.WeightKg && this._flags == other._flags;
		}

		public override bool Equals(object obj)
		{
			if (obj is PickupSyncInfo)
			{
				PickupSyncInfo pickupSyncInfo = (PickupSyncInfo)obj;
				return this.Equals(pickupSyncInfo);
			}
			return false;
		}

		public static PickupSyncInfo None = new PickupSyncInfo
		{
			ItemId = ItemType.None
		};

		public ItemType ItemId;

		public ushort Serial;

		public float WeightKg;

		private PickupSyncInfo.PickupFlags _flags;

		[Flags]
		private enum PickupFlags : byte
		{
			Locked = 1,
			InUse = 2
		}
	}
}
