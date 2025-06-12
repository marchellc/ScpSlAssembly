using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects;

public class BasicNonInteractableDoor : BasicDoor, INonInteractableDoor, IScp106PassableDoor
{
	[SerializeField]
	private bool _ignoreLockdowns;

	[SerializeField]
	private bool _ignoreRemoteAdmin;

	[SerializeField]
	[SyncVar]
	private bool _blockScp106;

	public bool IgnoreLockdowns => this._ignoreLockdowns;

	public bool IgnoreRemoteAdmin => this._ignoreRemoteAdmin;

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

	public bool Network_blockScp106
	{
		get
		{
			return this._blockScp106;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._blockScp106, 8uL, null);
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
			writer.WriteBool(this._blockScp106);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteBool(this._blockScp106);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._blockScp106, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._blockScp106, null, reader.ReadBool());
		}
	}
}
