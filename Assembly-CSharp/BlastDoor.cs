using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public class BlastDoor : NetworkBehaviour
{
	public static readonly HashSet<BlastDoor> Instances = new HashSet<BlastDoor>();

	private static readonly int _isOpenId = Animator.StringToHash("IsOpen");

	private Animator _animator;

	[SyncVar(hook = "SetDoorState")]
	private bool _isOpen = true;

	public bool IsOpen => this._isOpen;

	public bool Network_isOpen
	{
		get
		{
			return this._isOpen;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._isOpen, 1uL, SetDoorState);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		this.SetDoorState(_: true, this._isOpen);
	}

	private void Start()
	{
		BlastDoor.Instances.Add(this);
	}

	private void OnDestroy()
	{
		BlastDoor.Instances.Remove(this);
	}

	private void SetDoorState(bool _, bool newState)
	{
		this.Network_isOpen = newState;
		if (this._animator == null)
		{
			this._animator = base.GetComponent<Animator>();
		}
		this._animator.SetBool(BlastDoor._isOpenId, newState);
	}

	[Server]
	public void ServerSetTargetState(bool isOpen)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void BlastDoor::ServerSetTargetState(System.Boolean)' called when server was not active");
		}
		else
		{
			this.SetDoorState(this._isOpen, isOpen);
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
			writer.WriteBool(this._isOpen);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this._isOpen);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._isOpen, SetDoorState, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._isOpen, SetDoorState, reader.ReadBool());
		}
	}
}
