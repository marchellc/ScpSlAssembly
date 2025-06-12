using System.Runtime.InteropServices;
using Footprinting;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public abstract class TimeGrenade : ThrownProjectile
{
	[SerializeField]
	private float _fuseTime;

	[SyncVar]
	private double _syncTargetTime;

	private bool _alreadyDetonated;

	public double TargetTime
	{
		get
		{
			return this._syncTargetTime;
		}
		set
		{
			this.Network_syncTargetTime = value;
		}
	}

	public double Network_syncTargetTime
	{
		get
		{
			return this._syncTargetTime;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncTargetTime, 2uL, null);
		}
	}

	public virtual bool ServerFuseEnd()
	{
		ProjectileExplodingEventArgs e = new ProjectileExplodingEventArgs(this, base.PreviousOwner.Hub, base.transform.position);
		ServerEvents.OnProjectileExploding(e);
		if (!e.IsAllowed)
		{
			return false;
		}
		if (base.PreviousOwner.Hub != e.Player?.ReferenceHub)
		{
			base.PreviousOwner = new Footprint(e.Player?.ReferenceHub);
		}
		base.transform.position = e.Position;
		return true;
	}

	public override void ServerActivate()
	{
		this.Network_syncTargetTime = NetworkTime.time + (double)this._fuseTime;
	}

	protected virtual void Update()
	{
		if (NetworkServer.active && !this._alreadyDetonated && this.TargetTime != 0.0 && !(NetworkTime.time < this.TargetTime))
		{
			this.ServerFuseEnd();
			this._alreadyDetonated = true;
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
			writer.WriteDouble(this._syncTargetTime);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteDouble(this._syncTargetTime);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncTargetTime, null, reader.ReadDouble());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncTargetTime, null, reader.ReadDouble());
		}
	}
}
