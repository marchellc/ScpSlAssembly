using System;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using UnityEngine;

namespace PlayerStatsSystem
{
	public class HumeShieldStat : SyncedStatBase
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
				if (this._maxValueOverride >= this.MinValue)
				{
					return this._maxValueOverride;
				}
				HumeShieldModuleBase humeShieldModuleBase;
				if (!this.TryGetHsModule(out humeShieldModuleBase))
				{
					return 0f;
				}
				return humeShieldModuleBase.HsMax;
			}
			set
			{
				this._maxValueOverride = value;
				this.MaxValueDirty = true;
			}
		}

		public override float CurValue
		{
			get
			{
				return base.CurValue;
			}
			set
			{
				base.CurValue = Mathf.Max(0f, value);
			}
		}

		public override bool CheckDirty(float prevValue, float newValue)
		{
			return Mathf.CeilToInt(prevValue) != Mathf.CeilToInt(newValue);
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

		internal override void Update()
		{
			base.Update();
			HumeShieldModuleBase humeShieldModuleBase;
			if (!NetworkServer.active || !this.TryGetHsModule(out humeShieldModuleBase) || humeShieldModuleBase.HsRegeneration == 0f)
			{
				return;
			}
			float hsCurrent = humeShieldModuleBase.HsCurrent;
			float num = humeShieldModuleBase.HsRegeneration * Time.deltaTime;
			float num2 = ((this._maxValueOverride != -1f) ? this._maxValueOverride : humeShieldModuleBase.HsMax);
			if (num > 0f)
			{
				if (hsCurrent >= num2)
				{
					return;
				}
				this.CurValue = Mathf.MoveTowards(hsCurrent, num2, num);
				return;
			}
			else
			{
				if (hsCurrent <= 0f)
				{
					return;
				}
				this.CurValue = hsCurrent + num;
				return;
			}
		}

		internal override void ClassChanged()
		{
			base.ClassChanged();
			if (base.Hub.roleManager.CurrentRole is IHumeShieldedRole)
			{
				return;
			}
			this.MaxValue = float.MinValue;
			this.CurValue = 0f;
		}

		protected override void OnValueChanged(float prevValue, float newValue)
		{
			HumeShieldModuleBase humeShieldModuleBase;
			if (this.TryGetHsModule(out humeShieldModuleBase))
			{
				humeShieldModuleBase.OnHsValueChanged(prevValue, newValue);
			}
		}

		private bool TryGetHsModule(out HumeShieldModuleBase controller)
		{
			IHumeShieldedRole humeShieldedRole = base.Hub.roleManager.CurrentRole as IHumeShieldedRole;
			if (humeShieldedRole != null)
			{
				controller = humeShieldedRole.HumeShieldModule;
				return true;
			}
			controller = null;
			return false;
		}

		private float _maxValueOverride = float.MinValue;
	}
}
