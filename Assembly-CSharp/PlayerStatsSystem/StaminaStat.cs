using Mirror;
using PlayerRoles;
using UnityEngine;

namespace PlayerStatsSystem;

public class StaminaStat : SyncedStatBase
{
	private const SyncMode DefaultSyncMode = SyncMode.PrivateAndSpectators;

	private SyncMode _syncMode = SyncMode.PrivateAndSpectators;

	private RoleTypeId _overrideRole = RoleTypeId.None;

	private float _maxValue;

	public override SyncMode Mode => this._syncMode;

	public override float MinValue => 0f;

	public override float MaxValue
	{
		get
		{
			return this._maxValue;
		}
		set
		{
			this._maxValue = value;
			base.MaxValueDirty = true;
		}
	}

	public void ModifyAmount(float f)
	{
		this.CurValue = Mathf.Clamp01(this.CurValue + f);
	}

	public void ChangeSyncMode(SyncMode newMode)
	{
		this._syncMode = newMode;
		this._overrideRole = base.Hub.GetRoleId();
	}

	private byte ToByte(float val)
	{
		return (byte)Mathf.RoundToInt(Mathf.Clamp01(val) * 255f);
	}

	public override float ReadValue(SyncedStatMessages.StatMessageType type, NetworkReader reader)
	{
		return (float)(int)reader.ReadByte() / 255f;
	}

	public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
	{
		byte value = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? this.ToByte(this.CurValue) : this.ToByte(this.MaxValue));
		writer.WriteByte(value);
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
			this._syncMode = SyncMode.PrivateAndSpectators;
			this._overrideRole = RoleTypeId.None;
		}
		base.ClassChanged();
	}
}
