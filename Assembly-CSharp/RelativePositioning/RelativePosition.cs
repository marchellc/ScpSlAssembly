using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace RelativePositioning;

public struct RelativePosition : NetworkMessage, IEquatable<RelativePosition>
{
	private static readonly float InverseAccuracy = 0.00390625f;

	public const float Accuracy = 256f;

	public readonly short PositionX;

	public readonly short PositionY;

	public readonly short PositionZ;

	public readonly byte WaypointId;

	public bool OutOfRange;

	public Vector3 Position => WaypointBase.GetWorldPosition(this.WaypointId, this.Relative);

	internal Vector3 Relative => new Vector3((float)this.PositionX * RelativePosition.InverseAccuracy, (float)this.PositionY * RelativePosition.InverseAccuracy, (float)this.PositionZ * RelativePosition.InverseAccuracy);

	public RelativePosition(Vector3 targetPos)
	{
		WaypointBase.GetRelativePosition(targetPos, out this.WaypointId, out var rel);
		bool flag = RelativePosition.TryCompressPosition(rel.x, out this.PositionX);
		bool flag2 = RelativePosition.TryCompressPosition(rel.y, out this.PositionY);
		bool flag3 = RelativePosition.TryCompressPosition(rel.z, out this.PositionZ);
		this.OutOfRange = !flag || !flag2 || !flag3;
	}

	public RelativePosition(IFpcRole fpc)
		: this(fpc.FpcModule.Position)
	{
	}

	public RelativePosition(ReferenceHub hub)
		: this((hub.roleManager.CurrentRole is IFpcRole fpcRole) ? fpcRole.FpcModule.Position : hub.transform.position)
	{
	}

	public RelativePosition(NetworkReader reader)
	{
		this.WaypointId = reader.ReadByte();
		if (this.WaypointId > 0)
		{
			this.PositionX = reader.ReadShort();
			this.PositionY = reader.ReadShort();
			this.PositionZ = reader.ReadShort();
		}
		else
		{
			this.PositionX = 0;
			this.PositionY = 0;
			this.PositionZ = 0;
		}
		this.OutOfRange = false;
	}

	public RelativePosition(byte waypoint, short x, short y, short z, bool outOfRange)
	{
		this.WaypointId = waypoint;
		this.PositionX = x;
		this.PositionY = y;
		this.PositionZ = z;
		this.OutOfRange = outOfRange;
	}

	public void Write(NetworkWriter writer)
	{
		writer.WriteByte(this.WaypointId);
		if (this.WaypointId > 0)
		{
			writer.WriteShort(this.PositionX);
			writer.WriteShort(this.PositionY);
			writer.WriteShort(this.PositionZ);
		}
	}

	private static bool TryCompressPosition(float pos, out short compressed)
	{
		float num = pos * 256f;
		if (num < -32768f)
		{
			compressed = short.MinValue;
			return false;
		}
		if (num > 32767f)
		{
			compressed = short.MaxValue;
			return false;
		}
		compressed = (short)num;
		return true;
	}

	public bool Equals(RelativePosition other)
	{
		if (this.PositionX == other.PositionX && this.PositionY == other.PositionY && this.PositionZ == other.PositionZ)
		{
			return this.WaypointId == other.WaypointId;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is RelativePosition relativePosition)
		{
			return relativePosition.Equals(this);
		}
		return false;
	}

	public static bool operator ==(RelativePosition left, RelativePosition right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RelativePosition left, RelativePosition right)
	{
		return !left.Equals(right);
	}

	public override int GetHashCode()
	{
		return (((((this.WaypointId * 397) ^ this.PositionX) * 397) ^ this.PositionZ) * 397) ^ this.PositionY;
	}
}
