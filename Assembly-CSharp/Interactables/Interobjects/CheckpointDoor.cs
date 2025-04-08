using System;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class CheckpointDoor : DoorVariant, IDamageableDoor
	{
		public float MaxHealth
		{
			get
			{
				float num = 0f;
				int num2 = 0;
				DoorVariant[] subDoors = this._subDoors;
				for (int i = 0; i < subDoors.Length; i++)
				{
					IDamageableDoor damageableDoor = subDoors[i] as IDamageableDoor;
					if (damageableDoor != null)
					{
						num += damageableDoor.MaxHealth;
						num2++;
					}
				}
				return num / (float)num2;
			}
			set
			{
				DoorVariant[] subDoors = this._subDoors;
				for (int i = 0; i < subDoors.Length; i++)
				{
					BreakableDoor breakableDoor = subDoors[i] as BreakableDoor;
					if (breakableDoor != null)
					{
						breakableDoor.MaxHealth = value;
					}
				}
			}
		}

		public float RemainingHealth
		{
			get
			{
				float num = 0f;
				int num2 = 0;
				DoorVariant[] subDoors = this._subDoors;
				for (int i = 0; i < subDoors.Length; i++)
				{
					IDamageableDoor damageableDoor = subDoors[i] as IDamageableDoor;
					if (damageableDoor != null)
					{
						num += damageableDoor.RemainingHealth;
						num2++;
					}
				}
				return num / (float)num2;
			}
			set
			{
				DoorVariant[] subDoors = this._subDoors;
				for (int i = 0; i < subDoors.Length; i++)
				{
					BreakableDoor breakableDoor = subDoors[i] as BreakableDoor;
					if (breakableDoor != null)
					{
						breakableDoor.RemainingHealth = value;
					}
				}
			}
		}

		public bool IsDestroyed
		{
			get
			{
				foreach (DoorVariant doorVariant in this._subDoors)
				{
					if (doorVariant is IDamageableDoor && !(doorVariant as IDamageableDoor).IsDestroyed)
					{
						return false;
					}
				}
				return true;
			}
			set
			{
				DoorVariant[] subDoors = this._subDoors;
				for (int i = 0; i < subDoors.Length; i++)
				{
					IDamageableDoor damageableDoor = subDoors[i] as IDamageableDoor;
					if (damageableDoor != null)
					{
						damageableDoor.IsDestroyed = value;
					}
				}
			}
		}

		public DoorVariant[] SubDoors
		{
			get
			{
				return this._subDoors;
			}
		}

		public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
		{
			int num = 0;
			foreach (DoorVariant doorVariant in this._subDoors)
			{
				IDamageableDoor damageableDoor = doorVariant as IDamageableDoor;
				if (damageableDoor != null && damageableDoor.IsDestroyed)
				{
					num++;
				}
				else if (!doorVariant.AllowInteracting(null, colliderId))
				{
					return false;
				}
			}
			if (num >= this._subDoors.Length)
			{
				this.RpcPlayBeepSound(2);
				return false;
			}
			return this.CurrentSequence == CheckpointDoor.CheckpointSequenceStage.Idle;
		}

		public override float GetExactState()
		{
			if (this._subDoors.Length == 0)
			{
				return 0f;
			}
			float num = 0f;
			DoorVariant[] subDoors = this._subDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				float exactState = subDoors[i].GetExactState();
				if (num < exactState)
				{
					num = exactState;
				}
			}
			return num;
		}

		public override bool IsConsideredOpen()
		{
			DoorVariant[] subDoors = this._subDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i].IsConsideredOpen())
				{
					return true;
				}
			}
			return false;
		}

		public override void LockBypassDenied(ReferenceHub ply, byte colliderId)
		{
			this.RpcPlayBeepSound(1);
		}

		public override void PermissionsDenied(ReferenceHub ply, byte colliderId)
		{
			this.RpcPlayBeepSound(0);
		}

		public override bool AnticheatPassageApproved()
		{
			DoorVariant[] subDoors = this._subDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i].AnticheatPassageApproved())
				{
					return true;
				}
			}
			return false;
		}

		protected override void Awake()
		{
			base.Awake();
		}

		protected override void LockChanged(ushort prevValue)
		{
		}

		protected override void Update()
		{
			base.Update();
			this.UpdateSequence();
			if (!this._prevDestroyed && this.IsDestroyed)
			{
				this._prevDestroyed = true;
				this.ClientDestroyEffects();
			}
			if (this._prevDestroyed && !this.IsDestroyed)
			{
				this._prevDestroyed = false;
				this.ClientRepairEffects();
			}
		}

		private void UpdateSequence()
		{
			bool flag = ((DoorLockReason)this.ActiveLocks).HasFlagFast(DoorLockReason.DecontLockdown);
			bool flag2 = ((DoorLockReason)this.ActiveLocks).HasFlagFast(DoorLockReason.DecontEvacuate) || ((DoorLockReason)this.ActiveLocks).HasFlagFast(DoorLockReason.Warhead);
			if (this.TargetState && this.CurrentSequence == CheckpointDoor.CheckpointSequenceStage.Idle)
			{
				if (NetworkServer.active)
				{
					this.ToggleAllDoors(true);
				}
				this.CurrentSequence = CheckpointDoor.CheckpointSequenceStage.Granted;
				this.MainTimer = 0f;
				return;
			}
			switch (this.CurrentSequence)
			{
			case CheckpointDoor.CheckpointSequenceStage.Granted:
				this.MainTimer += Time.deltaTime;
				if (this.MainTimer > this.OpeningTime)
				{
					this.CurrentSequence = CheckpointDoor.CheckpointSequenceStage.Open;
					this.MainTimer = 0f;
					return;
				}
				break;
			case CheckpointDoor.CheckpointSequenceStage.Open:
				if (NetworkServer.active)
				{
					if (!flag2)
					{
						this.MainTimer += Time.deltaTime;
					}
					if (this.MainTimer > this.WaitTime || flag)
					{
						this.MainTimer = 0f;
						base.NetworkTargetState = false;
					}
				}
				if (!this.TargetState)
				{
					this.CurrentSequence = CheckpointDoor.CheckpointSequenceStage.Closing;
					return;
				}
				break;
			case CheckpointDoor.CheckpointSequenceStage.Closing:
				if (NetworkServer.active)
				{
					this.MainTimer += Time.deltaTime;
					if (this.MainTimer <= this.WarningTime && !flag)
					{
						return;
					}
					this.CurrentSequence = CheckpointDoor.CheckpointSequenceStage.Idle;
					this.ToggleAllDoors(false);
					if (!DoorLockUtils.GetMode((DoorLockReason)this.ActiveLocks).HasFlagFast(DoorLockMode.CanClose) && DoorLockUtils.GetMode((DoorLockReason)this.ActiveLocks).HasFlagFast(DoorLockMode.CanOpen))
					{
						base.NetworkTargetState = true;
						return;
					}
				}
				else
				{
					foreach (DoorVariant doorVariant in this._subDoors)
					{
						IDamageableDoor damageableDoor = doorVariant as IDamageableDoor;
						if ((damageableDoor == null || !damageableDoor.IsDestroyed) && doorVariant.GetExactState() >= 1f)
						{
							return;
						}
					}
					this.CurrentSequence = CheckpointDoor.CheckpointSequenceStage.Idle;
				}
				break;
			default:
				return;
			}
		}

		public void ToggleAllDoors(bool newState)
		{
			foreach (DoorVariant doorVariant in this._subDoors)
			{
				IDamageableDoor damageableDoor = doorVariant as IDamageableDoor;
				if (damageableDoor == null || !damageableDoor.IsDestroyed)
				{
					doorVariant.NetworkTargetState = newState;
				}
			}
		}

		[ClientRpc]
		public void RpcPlayBeepSound(byte deniedType)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteByte(deniedType);
			this.SendRPCInternal("System.Void Interactables.Interobjects.CheckpointDoor::RpcPlayBeepSound(System.Byte)", 1064856837, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public bool ServerDamage(float hp, DoorDamageType type, Footprint attacker = default(Footprint))
		{
			bool flag = false;
			DoorVariant[] subDoors = this._subDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				IDamageableDoor damageableDoor = subDoors[i] as IDamageableDoor;
				if (damageableDoor != null)
				{
					flag |= damageableDoor.ServerDamage(hp, type, attacker);
				}
			}
			return flag;
		}

		public bool ServerRepair()
		{
			bool flag = false;
			DoorVariant[] subDoors = this._subDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				IDamageableDoor damageableDoor = subDoors[i] as IDamageableDoor;
				if (damageableDoor != null)
				{
					flag |= damageableDoor.ServerRepair();
				}
			}
			return flag;
		}

		public float GetHealthPercent()
		{
			float num = 1f;
			DoorVariant[] subDoors = this._subDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				IDamageableDoor damageableDoor = subDoors[i] as IDamageableDoor;
				if (damageableDoor != null)
				{
					num *= damageableDoor.GetHealthPercent();
				}
			}
			return num;
		}

		public void ClientDestroyEffects()
		{
		}

		public void ClientRepairEffects()
		{
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcPlayBeepSound__Byte(byte deniedType)
		{
		}

		protected static void InvokeUserCode_RpcPlayBeepSound__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayBeepSound called on server.");
				return;
			}
			((CheckpointDoor)obj).UserCode_RpcPlayBeepSound__Byte(reader.ReadByte());
		}

		static CheckpointDoor()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(CheckpointDoor), "System.Void Interactables.Interobjects.CheckpointDoor::RpcPlayBeepSound(System.Byte)", new RemoteCallDelegate(CheckpointDoor.InvokeUserCode_RpcPlayBeepSound__Byte));
		}

		public float OpeningTime;

		public float WaitTime;

		public float WarningTime;

		[NonSerialized]
		public CheckpointDoor.CheckpointSequenceStage CurrentSequence;

		[NonSerialized]
		public float MainTimer;

		[SerializeField]
		private DoorVariant[] _subDoors;

		private bool _permanentDestroyment;

		private string _warningText;

		private bool _prevDestroyed;

		public enum CheckpointSequenceStage
		{
			Idle,
			Granted,
			Open,
			Closing
		}

		private enum CheckpointErrorType : byte
		{
			Denied,
			LockedDown,
			Destroyed
		}
	}
}
