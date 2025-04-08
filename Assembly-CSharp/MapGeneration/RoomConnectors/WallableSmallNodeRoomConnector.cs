using System;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace MapGeneration.RoomConnectors
{
	public class WallableSmallNodeRoomConnector : SpawnableRoomConnector
	{
		private Vector3 ForwardOrigin
		{
			get
			{
				return base.transform.TransformPoint(this._forwardWallRaycastOrigin);
			}
		}

		private Vector3 BackOrigin
		{
			get
			{
				return base.transform.TransformPoint(this._backWallRaycastOrigin);
			}
		}

		private void Start()
		{
			if (NetworkServer.active)
			{
				this.ServerDetectWalls();
			}
			this.UpdateMask(0, this._syncBitmask);
		}

		[Server]
		private void ServerDetectWalls()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void MapGeneration.RoomConnectors.WallableSmallNodeRoomConnector::ServerDetectWalls()' called when server was not active");
				return;
			}
			Vector3 forward = base.transform.forward;
			Vector3 vector = -forward;
			bool flag = this.CheckWall(this.ForwardOrigin, forward);
			bool flag2 = this.CheckWall(this.BackOrigin, vector);
			byte b = 0;
			if (!flag)
			{
				b |= 1;
			}
			if (!flag2)
			{
				b |= 2;
			}
			this.Network_syncBitmask = b;
		}

		private void UpdateMask(byte _, byte targetBitmask)
		{
			this._forwardWall.SetActive((targetBitmask & 1) > 0);
			this._backWall.SetActive((targetBitmask & 2) > 0);
		}

		private bool CheckWall(Vector3 origin, Vector3 dir)
		{
			return Physics.Raycast(origin, dir, 0.4f, WallableSmallNodeRoomConnector.DetectionMask);
		}

		private void OnDrawGizmosSelected()
		{
			double num = Time.realtimeSinceStartupAsDouble - (double)((int)Time.realtimeSinceStartupAsDouble);
			float num2 = 0.4f * (1f + (float)num) / 2f;
			Gizmos.color = Color.blue;
			Gizmos.DrawRay(this.ForwardOrigin, base.transform.forward * num2);
			Gizmos.color = Color.yellow;
			Gizmos.DrawRay(this.BackOrigin, -base.transform.forward * num2);
		}

		public override bool Weaved()
		{
			return true;
		}

		public byte Network_syncBitmask
		{
			get
			{
				return this._syncBitmask;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncBitmask, 1UL, new Action<byte, byte>(this.UpdateMask));
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteByte(this._syncBitmask);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteByte(this._syncBitmask);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncBitmask, new Action<byte, byte>(this.UpdateMask), reader.ReadByte());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncBitmask, new Action<byte, byte>(this.UpdateMask), reader.ReadByte());
			}
		}

		private static readonly CachedLayerMask DetectionMask = new CachedLayerMask(new string[] { "Default" });

		private const float RayLength = 0.4f;

		private const byte BitmaskForward = 1;

		private const byte BitmaskBack = 2;

		[Header("The blue arrow")]
		[SerializeField]
		private Vector3 _forwardWallRaycastOrigin;

		[Header("The yellow arrow")]
		[SerializeField]
		private Vector3 _backWallRaycastOrigin;

		[Space]
		[SerializeField]
		private GameObject _forwardWall;

		[SerializeField]
		private GameObject _backWall;

		[SyncVar(hook = "UpdateMask")]
		private byte _syncBitmask;
	}
}
