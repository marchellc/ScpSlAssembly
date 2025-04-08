using System;
using Mirror;
using PlayerRoles;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class HealthStat : SyncedStatBase
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
				if (value == this._maxValue)
				{
					return;
				}
				this._maxValue = value;
				this.MaxValueDirty = true;
			}
		}

		public bool FullyHealed
		{
			get
			{
				return this.CurValue >= this.MaxValue;
			}
		}

		public override float ReadValue(NetworkReader reader)
		{
			return (float)reader.ReadUShort();
		}

		public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
		{
			int num = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? Mathf.Clamp(Mathf.CeilToInt(this.CurValue), 0, 65535) : Mathf.Clamp(Mathf.CeilToInt(this.MaxValue), 0, 65535));
			writer.WriteUShort((ushort)num);
		}

		public override bool CheckDirty(float prevValue, float newValue)
		{
			return Mathf.CeilToInt(prevValue) != Mathf.CeilToInt(newValue);
		}

		internal override void ClassChanged()
		{
			base.ClassChanged();
			if (!NetworkServer.active)
			{
				return;
			}
			IHealthbarRole healthbarRole = base.Hub.roleManager.CurrentRole as IHealthbarRole;
			this.MaxValue = ((healthbarRole != null) ? healthbarRole.MaxHealth : 0f);
			this.CurValue = this.MaxValue;
		}

		public void ServerHeal(float healAmount)
		{
			this.CurValue = Mathf.Min(this.CurValue + Mathf.Abs(healAmount), this.MaxValue);
		}

		private float _maxValue;
	}
}
