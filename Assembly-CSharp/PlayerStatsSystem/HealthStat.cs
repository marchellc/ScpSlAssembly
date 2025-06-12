using Mirror;
using PlayerRoles;
using UnityEngine;

namespace PlayerStatsSystem;

public class HealthStat : SyncedStatBase
{
	private float _maxValue;

	public override SyncMode Mode => SyncMode.PrivateAndSpectators;

	public override float MinValue => 0f;

	public override float MaxValue
	{
		get
		{
			return this._maxValue;
		}
		set
		{
			if (value != this._maxValue)
			{
				this._maxValue = value;
				base.MaxValueDirty = true;
				this.CurValue = Mathf.Min(this.CurValue, this._maxValue);
			}
		}
	}

	public bool FullyHealed => this.CurValue >= this.MaxValue;

	public override float ReadValue(SyncedStatMessages.StatMessageType type, NetworkReader reader)
	{
		return (int)reader.ReadUShort();
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
		if (NetworkServer.active)
		{
			this.MaxValue = ((base.Hub.roleManager.CurrentRole is IHealthbarRole healthbarRole) ? healthbarRole.MaxHealth : 0f);
			this.CurValue = this.MaxValue;
		}
	}

	public void ServerHeal(float healAmount)
	{
		this.CurValue = Mathf.Min(this.CurValue + Mathf.Abs(healAmount), this.MaxValue);
	}
}
