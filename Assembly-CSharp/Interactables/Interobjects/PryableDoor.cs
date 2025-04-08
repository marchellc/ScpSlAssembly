using System;
using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class PryableDoor : BasicDoor, IScp106PassableDoor
	{
		public bool IsBeingPried
		{
			get
			{
				return this._isBeingPried;
			}
		}

		public bool IsScp106Passable
		{
			get
			{
				return !this._restrict106WhileLocked || this.ActiveLocks == 0 || this.TargetState;
			}
			set
			{
				this.Network_restrict106WhileLocked = !value;
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
			if (this._blockPryingMask != DoorLockReason.None && ((DoorLockReason)this.ActiveLocks).HasFlagFast(this._blockPryingMask))
			{
				return false;
			}
			if (!this.AllowInteracting(null, 0))
			{
				return false;
			}
			Scp096PryingGateEventArgs scp096PryingGateEventArgs = new Scp096PryingGateEventArgs(player, this);
			Scp096Events.OnPryingGate(scp096PryingGateEventArgs);
			if (!scp096PryingGateEventArgs.IsAllowed)
			{
				return false;
			}
			if (this.DoorName != null)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Door, ((player == null) ? "null" : player.LoggedNameFromRefHub()) + " pried " + this.DoorName + ".", ServerLogs.ServerLogType.GameEvent, false);
			}
			this.RpcPryGate();
			this._remainingPryCooldown = this._pryAnimDuration;
			this._isBeingPried = true;
			Scp096Events.OnPriedGate(new Scp096PriedGateEventArgs(player, this));
			return true;
		}

		[ClientRpc]
		public void RpcPryGate()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void Interactables.Interobjects.PryableDoor::RpcPryGate()", -166089162, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
		{
			return this._remainingPryCooldown <= 0f && base.AllowInteracting(ply, colliderId);
		}

		protected override void Update()
		{
			base.Update();
			if (this._remainingPryCooldown > 0f)
			{
				this._remainingPryCooldown -= Time.deltaTime;
				if (this._remainingPryCooldown <= 0f)
				{
					this.MainAnimator.ResetTrigger(PryableDoor.PryAnimHash);
					this._isBeingPried = false;
				}
			}
		}

		static PryableDoor()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(PryableDoor), "System.Void Interactables.Interobjects.PryableDoor::RpcPryGate()", new RemoteCallDelegate(PryableDoor.InvokeUserCode_RpcPryGate));
		}

		public override bool Weaved()
		{
			return true;
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
				base.GeneratedSyncVarSetter<bool>(value, ref this._restrict106WhileLocked, 8UL, null);
			}
		}

		protected void UserCode_RpcPryGate()
		{
			this._isBeingPried = true;
			this.MainAnimator.SetTrigger(PryableDoor.PryAnimHash);
			this.MainSource.PlayOneShot(this._prySound);
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
				return;
			}
			((PryableDoor)obj).UserCode_RpcPryGate();
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
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteBool(this._restrict106WhileLocked);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._restrict106WhileLocked, null, reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._restrict106WhileLocked, null, reader.ReadBool());
			}
		}

		private static readonly int PryAnimHash = Animator.StringToHash("PryGate");

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
	}
}
