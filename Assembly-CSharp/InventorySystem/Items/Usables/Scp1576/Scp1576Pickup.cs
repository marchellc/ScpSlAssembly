using System;
using System.Runtime.InteropServices;
using InventorySystem.Items.Pickups;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1576;

public class Scp1576Pickup : CollisionDetectionPickup
{
	private byte _prevSyncHorn;

	[SyncVar]
	private byte _syncHorn;

	[SerializeField]
	private Transform _horn;

	[SerializeField]
	private Vector3 _posZero;

	[SerializeField]
	private Vector3 _posOne;

	public float HornPos
	{
		get
		{
			return (float)(int)this._syncHorn / 255f;
		}
		set
		{
			this.Network_syncHorn = (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
		}
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
			base.GeneratedSyncVarSetter(value, ref this._syncHorn, 2uL, null);
		}
	}

	public static event Action<ushort, float> OnHornPositionUpdated;

	private void Update()
	{
		if (this._prevSyncHorn != this._syncHorn)
		{
			float hornPos = this.HornPos;
			this._horn.localPosition = Vector3.Lerp(this._posZero, this._posOne, hornPos);
			Scp1576Pickup.OnHornPositionUpdated?.Invoke(base.Info.Serial, hornPos);
			this._prevSyncHorn = this._syncHorn;
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
			NetworkWriterExtensions.WriteByte(writer, this._syncHorn);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncHorn);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncHorn, null, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncHorn, null, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}
