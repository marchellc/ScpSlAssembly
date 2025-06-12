using System;
using Mirror;
using RelativePositioning;

namespace PlayerRoles.FirstPersonControl.NetworkMessages;

public readonly struct FpcSyncData : IEquatable<FpcSyncData>
{
	private readonly PlayerMovementState _state;

	private readonly RelativePosition _position;

	private readonly ushort _rotH;

	private readonly ushort _rotV;

	private readonly bool _bitMouseLook;

	private readonly bool _bitPosition;

	private readonly bool _bitCustom;

	public FpcSyncData(NetworkReader reader)
	{
		Misc.ByteToBools(reader.ReadByte(), out var @bool, out var bool2, out var bool3, out var bool4, out var bool5, out this._bitMouseLook, out this._bitPosition, out this._bitCustom);
		this._state = (PlayerMovementState)Misc.BoolsToByte(@bool, bool2, bool3, bool4, bool5);
		this._position = (this._bitPosition ? reader.ReadRelativePosition() : new RelativePosition(0, 0, 0, 0, outOfRange: true));
		if (this._bitMouseLook)
		{
			this._rotH = reader.ReadUShort();
			this._rotV = reader.ReadUShort();
		}
		else
		{
			this._rotH = 0;
			this._rotV = 0;
		}
	}

	public FpcSyncData(FpcSyncData prev, PlayerMovementState state, bool bit, RelativePosition pos, FpcMouseLook mLook)
	{
		this._state = state;
		this._bitCustom = bit;
		this._position = pos;
		mLook.GetSyncValues(pos.WaypointId, out this._rotH, out this._rotV);
		this._bitPosition = prev._position != this._position;
		this._bitMouseLook = this._rotH != prev._rotH || this._rotV != prev._rotV;
	}

	public void Write(NetworkWriter writer)
	{
		Misc.ByteToBools((byte)this._state, out var @bool, out var bool2, out var bool3, out var bool4, out var bool5, out var _, out var _, out var _);
		writer.WriteByte(Misc.BoolsToByte(@bool, bool2, bool3, bool4, bool5, this._bitMouseLook, this._bitPosition, this._bitCustom));
		if (this._bitPosition)
		{
			writer.WriteRelativePosition(this._position);
		}
		if (this._bitMouseLook)
		{
			writer.WriteUShort(this._rotH);
			writer.WriteUShort(this._rotV);
		}
	}

	public bool TryApply(ReferenceHub hub, out FirstPersonMovementModule module, out bool bit)
	{
		bit = this._bitCustom;
		if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole) || !fpcRole.FpcModule.ModuleReady)
		{
			module = null;
			return false;
		}
		module = fpcRole.FpcModule;
		module.CurrentMovementState = this._state;
		FpcMotor motor = module.Motor;
		motor.MovementDetected = this._bitPosition;
		if (this._bitPosition)
		{
			motor.ReceivedPosition = this._position;
		}
		if (this._bitMouseLook)
		{
			module.MouseLook.ApplySyncValues(this._rotH, this._rotV);
		}
		return true;
	}

	public bool Equals(FpcSyncData other)
	{
		if (this._state == other._state && this._position == other._position && this._rotH == other._rotH)
		{
			return this._rotV == other._rotV;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is FpcSyncData fpcSyncData)
		{
			return fpcSyncData.Equals(this);
		}
		return false;
	}

	public static bool operator ==(FpcSyncData left, FpcSyncData right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(FpcSyncData left, FpcSyncData right)
	{
		return !left.Equals(right);
	}

	public override int GetHashCode()
	{
		return (int)((((((uint)(this._position.GetHashCode() * 397) ^ (uint)this._state) * 397) ^ this._rotH) * 397) ^ this._rotV);
	}
}
