using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects;

public class PryableDoor : BasicDoor, IScp106PassableDoor
{
	private static readonly int PryAnimHash;

	public Transform[] PryPositions;

	[SerializeField]
	private AudioClip _prySound;

	[SerializeField]
	private DoorLockReason _blockPryingMask;

	[SerializeField]
	private float _pryAnimDuration;

	[SerializeField]
	[SyncVar]
	private bool _restrict106WhileLocked;

	private float _remainingPryCooldown;

	private bool _isBeingPried;

	public bool IsBeingPried => this._isBeingPried;

	public bool IsScp106Passable
	{
		get
		{
			if (this._restrict106WhileLocked && base.ActiveLocks != 0)
			{
				return base.TargetState;
			}
			return true;
		}
		set
		{
			this.Network_restrict106WhileLocked = !value;
		}
	}

	public bool Network_restrict106WhileLocked
	{
		get
		{
			return this._restrict106WhileLocked;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._restrict106WhileLocked, 8uL, null);
		}
	}

	[Server]
	public bool TryPryGate(ReferenceHub player)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean Interactables.Interobjects.PryableDoor::TryPryGate(ReferenceHub)' called when server was not active");
			return default(bool);
		}
		if (this._blockPryingMask != DoorLockReason.None && ((DoorLockReason)base.ActiveLocks).HasFlagFast(this._blockPryingMask))
		{
			return false;
		}
		if (this.AllowInteracting(null, 0))
		{
			Scp096PryingGateEventArgs e = new Scp096PryingGateEventArgs(player, this);
			Scp096Events.OnPryingGate(e);
			if (!e.IsAllowed)
			{
				return false;
			}
			if (base.DoorName != null)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Door, ((player == null) ? "null" : player.LoggedNameFromRefHub()) + " pried " + base.DoorName + ".", ServerLogs.ServerLogType.GameEvent);
			}
			this.RpcPryGate();
			this._remainingPryCooldown = this._pryAnimDuration;
			this._isBeingPried = true;
			Scp096Events.OnPriedGate(new Scp096PriedGateEventArgs(player, this));
			return true;
		}
		return false;
	}

	[ClientRpc]
	public void RpcPryGate()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Interactables.Interobjects.PryableDoor::RpcPryGate()", -166089162, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
	{
		if (this._remainingPryCooldown <= 0f)
		{
			return base.AllowInteracting(ply, colliderId);
		}
		return false;
	}

	protected override void Update()
	{
		base.Update();
		if (this._remainingPryCooldown > 0f)
		{
			this._remainingPryCooldown -= Time.deltaTime;
			if (this._remainingPryCooldown <= 0f)
			{
				base.MainAnimator.ResetTrigger(PryableDoor.PryAnimHash);
				this._isBeingPried = false;
			}
		}
	}

	static PryableDoor()
	{
		PryableDoor.PryAnimHash = Animator.StringToHash("PryGate");
		RemoteProcedureCalls.RegisterRpc(typeof(PryableDoor), "System.Void Interactables.Interobjects.PryableDoor::RpcPryGate()", InvokeUserCode_RpcPryGate);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcPryGate()
	{
		this._isBeingPried = true;
		base.MainAnimator.SetTrigger(PryableDoor.PryAnimHash);
		base.MainSource.PlayOneShot(this._prySound);
		Timing.CallDelayed(this._pryAnimDuration, delegate
		{
			this._isBeingPried = false;
		}, base.gameObject);
	}

	protected static void InvokeUserCode_RpcPryGate(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPryGate called on server.");
		}
		else
		{
			((PryableDoor)obj).UserCode_RpcPryGate();
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this._restrict106WhileLocked);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteBool(this._restrict106WhileLocked);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._restrict106WhileLocked, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._restrict106WhileLocked, null, reader.ReadBool());
		}
	}
}
