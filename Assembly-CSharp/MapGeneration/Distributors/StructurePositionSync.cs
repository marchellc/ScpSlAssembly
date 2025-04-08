using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class StructurePositionSync : NetworkBehaviour
	{
		private void Start()
		{
			if (NetworkServer.active)
			{
				this.Network_position = base.transform.position;
				this.Network_rotationY = (sbyte)Mathf.RoundToInt(base.transform.rotation.eulerAngles.y / 5.625f);
				base.enabled = false;
			}
		}

		private void Update()
		{
			if (this._position != Vector3.zero)
			{
				base.transform.position = this._position;
				base.transform.rotation = Quaternion.Euler(Vector3.up * (float)this._rotationY * 5.625f);
				base.enabled = false;
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public sbyte Network_rotationY
		{
			get
			{
				return this._rotationY;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<sbyte>(value, ref this._rotationY, 1UL, null);
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
				base.GeneratedSyncVarSetter<Vector3>(value, ref this._position, 2UL, null);
			}
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
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteSByte(this._rotationY);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteVector3(this._position);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<sbyte>(ref this._rotationY, null, reader.ReadSByte());
				base.GeneratedSyncVarDeserialize<Vector3>(ref this._position, null, reader.ReadVector3());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<sbyte>(ref this._rotationY, null, reader.ReadSByte());
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Vector3>(ref this._position, null, reader.ReadVector3());
			}
		}

		public const float ConversionRate = 5.625f;

		[SyncVar]
		private sbyte _rotationY;

		[SyncVar]
		private Vector3 _position;
	}
}
