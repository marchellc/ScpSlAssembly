using System;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1576
{
	public class Scp1576Pickup : CollisionDetectionPickup
	{
		public static event Action<ushort, float> OnHornPositionUpdated;

		public float HornPos
		{
			get
			{
				return (float)this._syncHorn / 255f;
			}
			set
			{
				this.Network_syncHorn = (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
			}
		}

		private void Update()
		{
			if (this._prevSyncHorn == this._syncHorn)
			{
				return;
			}
			float hornPos = this.HornPos;
			this._horn.localPosition = Vector3.Lerp(this._posZero, this._posOne, hornPos);
			Action<ushort, float> onHornPositionUpdated = Scp1576Pickup.OnHornPositionUpdated;
			if (onHornPositionUpdated != null)
			{
				onHornPositionUpdated(this.Info.Serial, hornPos);
			}
			this._prevSyncHorn = this._syncHorn;
		}

		public override bool Weaved()
		{
			return true;
		}

		public byte Network_syncHorn
		{
			get
			{
				return this._syncHorn;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncHorn, 2UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteByte(this._syncHorn);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteByte(this._syncHorn);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncHorn, null, reader.ReadByte());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncHorn, null, reader.ReadByte());
			}
		}

		private byte _prevSyncHorn;

		[SyncVar]
		private byte _syncHorn;

		[SerializeField]
		private Transform _horn;

		[SerializeField]
		private Vector3 _posZero;

		[SerializeField]
		private Vector3 _posOne;
	}
}
