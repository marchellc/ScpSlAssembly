using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace MapGeneration.Distributors;

[DefaultExecutionOrder(-1)]
public class StructurePositionSync : NetworkBehaviour
{
	public const float ConversionRate = 5.625f;

	[SyncVar]
	private sbyte _rotationY;

	[SyncVar]
	private Vector3 _position;

	public sbyte Network_rotationY
	{
		get
		{
			return this._rotationY;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._rotationY, 1uL, null);
		}
	}

	public Vector3 Network_position
	{
		get
		{
			return this._position;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._position, 2uL, null);
		}
	}

	private void Start()
	{
		Transform transform = base.transform;
		if (NetworkServer.active)
		{
			transform.GetPositionAndRotation(out this._position, out var rotation);
			this.Network_rotationY = (sbyte)Mathf.RoundToInt(rotation.eulerAngles.y / 5.625f);
		}
		else
		{
			Quaternion rotation2 = Quaternion.Euler((float)this._rotationY * 5.625f * Vector3.up);
			transform.SetPositionAndRotation(this._position, rotation2);
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteSByte(this._rotationY);
			writer.WriteVector3(this._position);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteSByte(this._rotationY);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteVector3(this._position);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._rotationY, null, reader.ReadSByte());
			base.GeneratedSyncVarDeserialize(ref this._position, null, reader.ReadVector3());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._rotationY, null, reader.ReadSByte());
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._position, null, reader.ReadVector3());
		}
	}
}
