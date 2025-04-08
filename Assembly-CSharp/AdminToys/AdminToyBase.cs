using System;
using System.Runtime.InteropServices;
using Footprinting;
using Mirror;
using UnityEngine;

namespace AdminToys
{
	public abstract class AdminToyBase : NetworkBehaviour
	{
		public abstract string CommandName { get; }

		public Footprint SpawnerFootprint { get; private set; }

		public Vector3 RotationEuler
		{
			get
			{
				return this.Rotation.eulerAngles;
			}
			set
			{
				NetworkRotation = Quaternion.Euler(value);
			}
		}

		protected virtual void LateUpdate()
		{
			if (NetworkServer.active)
			{
				this.UpdatePositionServer();
				return;
			}
			this.UpdatePositionClient();
		}

		public virtual void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
		{
			this.SpawnerFootprint = new Footprint(admin);
			NetworkServer.Spawn(base.gameObject, null);
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} spawned an admin toy: {1} with NetID {2}.", admin.LoggedNameFromRefHub(), this.CommandName, base.netId), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
		}

		private void UpdatePositionServer()
		{
			if (this.IsStatic)
			{
				return;
			}
			this.NetworkPosition = base.transform.position;
			this.NetworkRotation = base.transform.rotation;
			this.NetworkScale = base.transform.localScale;
		}

		private void UpdatePositionClient()
		{
			if (this.IsStatic)
			{
				return;
			}
			Vector3 vector;
			Quaternion quaternion;
			Vector3 vector2;
			if (this.MovementSmoothing == 0)
			{
				vector = this.Position;
				quaternion = this.Rotation;
				vector2 = this.Scale;
			}
			else
			{
				float num = Time.deltaTime * (float)this.MovementSmoothing * 0.3f;
				vector = Vector3.Lerp(base.transform.position, this.Position, num);
				quaternion = Quaternion.Lerp(base.transform.rotation, this.Rotation, num);
				vector2 = Vector3.Lerp(base.transform.localScale, this.Scale, num);
			}
			base.transform.SetPositionAndRotation(vector, quaternion);
			base.transform.localScale = vector2;
		}

		public override bool Weaved()
		{
			return true;
		}

		public Vector3 NetworkPosition
		{
			get
			{
				return this.Position;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<Vector3>(value, ref this.Position, 1UL, null);
			}
		}

		public Quaternion NetworkRotation
		{
			get
			{
				return this.Rotation;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<Quaternion>(value, ref this.Rotation, 2UL, null);
			}
		}

		public Vector3 NetworkScale
		{
			get
			{
				return this.Scale;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<Vector3>(value, ref this.Scale, 4UL, null);
			}
		}

		public byte NetworkMovementSmoothing
		{
			get
			{
				return this.MovementSmoothing;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this.MovementSmoothing, 8UL, null);
			}
		}

		public bool NetworkIsStatic
		{
			get
			{
				return this.IsStatic;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this.IsStatic, 16UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteVector3(this.Position);
				writer.WriteQuaternion(this.Rotation);
				writer.WriteVector3(this.Scale);
				writer.WriteByte(this.MovementSmoothing);
				writer.WriteBool(this.IsStatic);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteVector3(this.Position);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteQuaternion(this.Rotation);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteVector3(this.Scale);
			}
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteByte(this.MovementSmoothing);
			}
			if ((base.syncVarDirtyBits & 16UL) != 0UL)
			{
				writer.WriteBool(this.IsStatic);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<Vector3>(ref this.Position, null, reader.ReadVector3());
				base.GeneratedSyncVarDeserialize<Quaternion>(ref this.Rotation, null, reader.ReadQuaternion());
				base.GeneratedSyncVarDeserialize<Vector3>(ref this.Scale, null, reader.ReadVector3());
				base.GeneratedSyncVarDeserialize<byte>(ref this.MovementSmoothing, null, reader.ReadByte());
				base.GeneratedSyncVarDeserialize<bool>(ref this.IsStatic, null, reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Vector3>(ref this.Position, null, reader.ReadVector3());
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Quaternion>(ref this.Rotation, null, reader.ReadQuaternion());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Vector3>(ref this.Scale, null, reader.ReadVector3());
			}
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this.MovementSmoothing, null, reader.ReadByte());
			}
			if ((num & 16L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this.IsStatic, null, reader.ReadBool());
			}
		}

		[SyncVar]
		public Vector3 Position;

		[SyncVar]
		public Quaternion Rotation;

		[SyncVar]
		public Vector3 Scale;

		[SyncVar]
		public byte MovementSmoothing;

		[SyncVar]
		public bool IsStatic;

		private const float SmoothingMultiplier = 0.3f;
	}
}
