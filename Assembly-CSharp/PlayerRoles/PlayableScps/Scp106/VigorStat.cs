using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public class VigorStat : SyncedStatBase
{
	private const float StartAmount = 0f;

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
			this._maxValue = value;
		}
	}

	public override bool CheckDirty(float prevValue, float newValue)
	{
		return this.ToByte(prevValue) != this.ToByte(newValue);
	}

	public override float ReadValue(SyncedStatMessages.StatMessageType type, NetworkReader reader)
	{
		return this.ToFloat(reader.ReadByte());
	}

	public override void WriteValue(SyncedStatMessages.StatMessageType type, NetworkWriter writer)
	{
		byte value = ((type == SyncedStatMessages.StatMessageType.CurrentValue) ? this.ToByte(this.CurValue) : this.ToByte(this.MaxValue));
		writer.WriteByte(value);
	}

	internal override void ClassChanged()
	{
		base.ClassChanged();
		if (NetworkServer.active)
		{
			this._maxValue = 1f;
			this.CurValue = 0f;
		}
	}

	private byte ToByte(float val)
	{
		return (byte)Mathf.CeilToInt(val * 255f);
	}

	private float ToFloat(byte val)
	{
		return (float)(int)val / 255f;
	}
}
