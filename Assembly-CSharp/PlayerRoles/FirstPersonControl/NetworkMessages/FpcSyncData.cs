using System;
using Mirror;
using RelativePositioning;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public readonly struct FpcSyncData : IEquatable<FpcSyncData>
	{
		public FpcSyncData(NetworkReader reader)
		{
			bool flag;
			bool flag2;
			bool flag3;
			bool flag4;
			bool flag5;
			Misc.ByteToBools(reader.ReadByte(), out flag, out flag2, out flag3, out flag4, out flag5, out this._bitMouseLook, out this._bitPosition, out this._bitCustom);
			this._state = (PlayerMovementState)Misc.BoolsToByte(flag, flag2, flag3, flag4, flag5, false, false, false);
			this._position = (this._bitPosition ? reader.ReadRelativePosition() : new RelativePosition(0, 0, 0, 0, true));
			if (this._bitMouseLook)
			{
				this._rotH = reader.ReadUShort();
				this._rotV = reader.ReadUShort();
				return;
			}
			this._rotH = 0;
			this._rotV = 0;
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
			bool flag;
			bool flag2;
			bool flag3;
			bool flag4;
			bool flag5;
			bool flag6;
			bool flag7;
			bool flag8;
			Misc.ByteToBools((byte)this._state, out flag, out flag2, out flag3, out flag4, out flag5, out flag6, out flag7, out flag8);
			writer.WriteByte(Misc.BoolsToByte(flag, flag2, flag3, flag4, flag5, this._bitMouseLook, this._bitPosition, this._bitCustom));
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
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null || !fpcRole.FpcModule.ModuleReady)
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
			return this._state == other._state && this._position == other._position && this._rotH == other._rotH && this._rotV == other._rotV;
		}

		public override bool Equals(object obj)
		{
			return obj is FpcSyncData && ((FpcSyncData)obj).Equals(this);
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
			return (((((this._position.GetHashCode() * 397) ^ (int)this._state) * 397) ^ (int)this._rotH) * 397) ^ (int)this._rotV;
		}

		private readonly PlayerMovementState _state;

		private readonly RelativePosition _position;

		private readonly ushort _rotH;

		private readonly ushort _rotV;

		private readonly bool _bitMouseLook;

		private readonly bool _bitPosition;

		private readonly bool _bitCustom;
	}
}
