using System;
using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class BasicNonInteractableDoor : BasicDoor, INonInteractableDoor, IScp106PassableDoor
	{
		public bool IgnoreLockdowns
		{
			get
			{
				return this._ignoreLockdowns;
			}
		}

		public bool IgnoreRemoteAdmin
		{
			get
			{
				return this._ignoreRemoteAdmin;
			}
		}

		public bool IsScp106Passable
		{
			get
			{
				return !this._blockScp106;
			}
			set
			{
				this.Network_blockScp106 = !value;
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public bool Network_blockScp106
		{
			get
			{
				return this._blockScp106;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this._blockScp106, 8UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteBool(this._blockScp106);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteBool(this._blockScp106);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._blockScp106, null, reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._blockScp106, null, reader.ReadBool());
			}
		}

		[SerializeField]
		private bool _ignoreLockdowns;

		[SerializeField]
		private bool _ignoreRemoteAdmin;

		[SerializeField]
		[SyncVar]
		private bool _blockScp106;
	}
}
