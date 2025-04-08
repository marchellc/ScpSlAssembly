using System;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class StaminaStat : SyncedStatBase
	{
		public override SyncedStatBase.SyncMode Mode
		{
			get
			{
				return this._syncMode;
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
				this.MaxValueDirty = true;
			}
		}

		public void ModifyAmount(float f)
		{
			this.CurValue = Mathf.Clamp01(this.CurValue + f);
		}

		public void ChangeSyncMode(SyncedStatBase.SyncMode newMode)
		{
			this._syncMode = newMode;
			this._overrideRole = base.Hub.GetRoleId();
		}

		private byte ToByte(float val)
		{
			return (byte)Mathf.RoundToInt(Mathf.Clamp01(val) * 255f);
		}

		public override float ReadValue(NetworkReader reader)
		{
			return (float)reader.ReadByte() / 255f;
		}

		public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
		{
			byte b = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? this.ToByte(this.CurValue) : this.ToByte(this.MaxValue));
			writer.WriteByte(b);
		}

		public override bool CheckDirty(float prevValue, float newValue)
		{
			return this.ToByte(prevValue) != this.ToByte(newValue);
		}

		internal override void ClassChanged()
		{
			this._maxValue = 1f;
			this.CurValue = this.MaxValue;
			if (this._overrideRole != RoleTypeId.None && base.Hub.GetRoleId() != this._overrideRole)
			{
				this._syncMode = SyncedStatBase.SyncMode.PrivateAndSpectators;
				this._overrideRole = RoleTypeId.None;
			}
			base.ClassChanged();
		}

		private const SyncedStatBase.SyncMode DefaultSyncMode = SyncedStatBase.SyncMode.PrivateAndSpectators;

		private SyncedStatBase.SyncMode _syncMode = SyncedStatBase.SyncMode.PrivateAndSpectators;

		private RoleTypeId _overrideRole = RoleTypeId.None;

		private float _maxValue;
	}
}
