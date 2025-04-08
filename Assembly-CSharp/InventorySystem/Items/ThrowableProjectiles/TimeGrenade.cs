using System;
using System.Runtime.InteropServices;
using Footprinting;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public abstract class TimeGrenade : ThrownProjectile
	{
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

		public virtual bool ServerFuseEnd()
		{
			ProjectileExplodingEventArgs projectileExplodingEventArgs = new ProjectileExplodingEventArgs(this, this.PreviousOwner.Hub, base.transform.position);
			ServerEvents.OnProjectileExploding(projectileExplodingEventArgs);
			if (!projectileExplodingEventArgs.IsAllowed)
			{
				return false;
			}
			ReferenceHub hub = this.PreviousOwner.Hub;
			Player player = projectileExplodingEventArgs.Player;
			if (hub != ((player != null) ? player.ReferenceHub : null))
			{
				Player player2 = projectileExplodingEventArgs.Player;
				this.PreviousOwner = new Footprint((player2 != null) ? player2.ReferenceHub : null);
			}
			base.transform.position = projectileExplodingEventArgs.Position;
			return true;
		}

		public override void ServerActivate()
		{
			this.Network_syncTargetTime = NetworkTime.time + (double)this._fuseTime;
		}

		protected virtual void Update()
		{
			if (!NetworkServer.active || this._alreadyDetonated || this.TargetTime == 0.0 || NetworkTime.time < this.TargetTime)
			{
				return;
			}
			this.ServerFuseEnd();
			this._alreadyDetonated = true;
		}

		public override bool Weaved()
		{
			return true;
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
				base.GeneratedSyncVarSetter<double>(value, ref this._syncTargetTime, 2UL, null);
			}
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
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteDouble(this._syncTargetTime);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<double>(ref this._syncTargetTime, null, reader.ReadDouble());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<double>(ref this._syncTargetTime, null, reader.ReadDouble());
			}
		}

		[SerializeField]
		private float _fuseTime;

		[SyncVar]
		private double _syncTargetTime;

		private bool _alreadyDetonated;
	}
}
