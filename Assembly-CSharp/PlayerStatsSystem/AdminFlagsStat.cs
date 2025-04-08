using System;
using Mirror;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class AdminFlagsStat : SyncedStatBase
	{
		public override SyncedStatBase.SyncMode Mode
		{
			get
			{
				return SyncedStatBase.SyncMode.Public;
			}
		}

		public override float MinValue
		{
			get
			{
				return 0f;
			}
		}

		public override float MaxValue
		{
			get
			{
				return this._maxValue;
			}
			set
			{
				this._maxValue = value;
			}
		}

		public AdminFlags Flags
		{
			get
			{
				return (AdminFlags)Mathf.RoundToInt(this.CurValue);
			}
			set
			{
				this.CurValue = (float)value;
			}
		}

		public bool HasFlag(AdminFlags flag)
		{
			return (flag & this.Flags) == flag;
		}

		public void InvertFlag(AdminFlags flag)
		{
			AdminFlags flags = this.Flags;
			this.Flags = (((flag & flags) != flag) ? (flags | flag) : (flags & ~flag));
		}

		public void SetFlag(AdminFlags flag, bool status)
		{
			this.Flags = (status ? (this.Flags | flag) : (this.Flags & ~flag));
		}

		public override float ReadValue(NetworkReader reader)
		{
			return (float)reader.ReadByte();
		}

		public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
		{
			byte b = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? ((byte)this.Flags) : ((byte)this.MaxValue));
			writer.WriteByte(b);
		}

		public override bool CheckDirty(float prevValue, float newValue)
		{
			return (int)prevValue != (int)newValue;
		}

		private float _maxValue;
	}
}
