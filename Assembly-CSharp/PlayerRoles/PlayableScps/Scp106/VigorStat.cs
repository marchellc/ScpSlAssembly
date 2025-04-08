using System;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class VigorStat : SyncedStatBase
	{
		public override SyncedStatBase.SyncMode Mode
		{
			get
			{
				return SyncedStatBase.SyncMode.PrivateAndSpectators;
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

		public override bool CheckDirty(float prevValue, float newValue)
		{
			return this.ToByte(prevValue) != this.ToByte(newValue);
		}

		public override float ReadValue(NetworkReader reader)
		{
			return this.ToFloat(reader.ReadByte());
		}

		public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
		{
			byte b = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? this.ToByte(this.CurValue) : this.ToByte(this.MaxValue));
			writer.WriteByte(b);
		}

		internal override void ClassChanged()
		{
			base.ClassChanged();
			if (!NetworkServer.active)
			{
				return;
			}
			this._maxValue = 1f;
			this.CurValue = 0f;
		}

		private byte ToByte(float val)
		{
			return (byte)Mathf.CeilToInt(val * 255f);
		}

		private float ToFloat(byte val)
		{
			return (float)val / 255f;
		}

		private const float StartAmount = 0f;

		private float _maxValue;
	}
}
