using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using InventorySystem.Items.Keycards;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public class Locker : SpawnableStructure, IServerInteractable, IInteractable
	{
		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			LockerChamber lockerChamber;
			if (!this.Chambers.TryGet((int)colliderId, out lockerChamber))
			{
				return;
			}
			if (!lockerChamber.CanInteract)
			{
				return;
			}
			bool flag = !this.CheckTogglePerms((int)colliderId, ply) && !ply.serverRoles.BypassMode;
			PlayerInteractingLockerEventArgs playerInteractingLockerEventArgs = new PlayerInteractingLockerEventArgs(ply, this, this.Chambers[(int)colliderId], !flag);
			PlayerEvents.OnInteractingLocker(playerInteractingLockerEventArgs);
			if (!playerInteractingLockerEventArgs.IsAllowed)
			{
				return;
			}
			flag = !playerInteractingLockerEventArgs.CanOpen;
			if (flag)
			{
				this.RpcPlayDenied(colliderId);
				PlayerEvents.OnInteractedLocker(new PlayerInteractedLockerEventArgs(ply, this, this.Chambers[(int)colliderId], !flag));
				return;
			}
			lockerChamber.SetDoor(!lockerChamber.IsOpen, this._grantedBeep);
			this.RefreshOpenedSyncvar();
			PlayerEvents.OnInteractedLocker(new PlayerInteractedLockerEventArgs(ply, this, this.Chambers[(int)colliderId], !flag));
		}

		public void RefreshOpenedSyncvar()
		{
			int num = 1;
			int num2 = 0;
			LockerChamber[] chambers = this.Chambers;
			for (int i = 0; i < chambers.Length; i++)
			{
				if (chambers[i].IsOpen)
				{
					num2 += num;
				}
				num *= 2;
			}
			if (num2 != (int)this.OpenedChambers)
			{
				this.NetworkOpenedChambers = (ushort)num2;
			}
		}

		public virtual void FillChamber(LockerChamber ch)
		{
			List<int> list = ListPool<int>.Shared.Rent();
			for (int i = 0; i < this.Loot.Length; i++)
			{
				LockerLoot lockerLoot = this.Loot[i];
				if (lockerLoot.RemainingUses > 0 && (ch.AcceptableItems.Length == 0 || ch.AcceptableItems.Contains(lockerLoot.TargetItem)))
				{
					for (int j = 0; j <= lockerLoot.ProbabilityPoints; j++)
					{
						list.Add(i);
					}
				}
			}
			if (list.Count > 0)
			{
				int num = list[global::UnityEngine.Random.Range(0, list.Count)];
				LockerLoot lockerLoot2 = this.Loot[num];
				ch.SpawnItem(lockerLoot2.TargetItem, global::UnityEngine.Random.Range(lockerLoot2.MinPerChamber, lockerLoot2.MaxPerChamber + 1));
				lockerLoot2.RemainingUses--;
			}
			ListPool<int>.Shared.Return(list);
		}

		[ClientRpc]
		public void RpcPlayDenied(byte chamberId)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteByte(chamberId);
			this.SendRPCInternal("System.Void MapGeneration.Distributors.Locker::RpcPlayDenied(System.Byte)", 1695236274, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		protected virtual void Update()
		{
			if (!NetworkClient.ready)
			{
				return;
			}
			if (NetworkServer.active && !this._serverChambersFilled)
			{
				this.ServerFillChambers();
				this._serverChambersFilled = true;
			}
			ushort? prevOpened = this._prevOpened;
			int? num = ((prevOpened != null) ? new int?((int)prevOpened.GetValueOrDefault()) : null);
			int i = (int)this.OpenedChambers;
			if ((num.GetValueOrDefault() == i) & (num != null))
			{
				return;
			}
			int num2 = 1;
			foreach (LockerChamber lockerChamber in this.Chambers)
			{
				lockerChamber.SetDoor(((int)this.OpenedChambers & num2) == num2 || !lockerChamber.AnimatorSet, this._grantedBeep);
				num2 *= 2;
			}
			this._prevOpened = new ushort?(this.OpenedChambers);
		}

		protected virtual void ServerFillChambers()
		{
			List<LockerChamber> list = new List<LockerChamber>(this.Chambers);
			if (this.MinChambersToFill != 0 && this.MaxChambersToFill >= this.MinChambersToFill)
			{
				int num = this.Chambers.Length - global::UnityEngine.Random.Range(this.MinChambersToFill, this.MaxChambersToFill + 1);
				for (int i = 0; i < num; i++)
				{
					list.RemoveAt(global::UnityEngine.Random.Range(0, list.Count));
				}
			}
			foreach (LockerChamber lockerChamber in list)
			{
				this.FillChamber(lockerChamber);
			}
		}

		protected virtual bool CheckTogglePerms(int chamberId, ReferenceHub ply)
		{
			KeycardPermissions requiredPermissions = this.Chambers[chamberId].RequiredPermissions;
			if (requiredPermissions > KeycardPermissions.None)
			{
				if (ply.inventory.CurInstance == null)
				{
					return false;
				}
				KeycardItem keycardItem = ply.inventory.CurInstance as KeycardItem;
				if (keycardItem == null)
				{
					return false;
				}
				if (!keycardItem.Permissions.HasFlagFast(requiredPermissions))
				{
					return false;
				}
			}
			return true;
		}

		public override bool Weaved()
		{
			return true;
		}

		public ushort NetworkOpenedChambers
		{
			get
			{
				return this.OpenedChambers;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<ushort>(value, ref this.OpenedChambers, 1UL, null);
			}
		}

		protected void UserCode_RpcPlayDenied__Byte(byte chamberId)
		{
			if ((int)chamberId > this.Chambers.Length)
			{
				return;
			}
			this.Chambers[(int)chamberId].PlayDenied(this._deniedBeep);
		}

		protected static void InvokeUserCode_RpcPlayDenied__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayDenied called on server.");
				return;
			}
			((Locker)obj).UserCode_RpcPlayDenied__Byte(reader.ReadByte());
		}

		static Locker()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Locker), "System.Void MapGeneration.Distributors.Locker::RpcPlayDenied(System.Byte)", new RemoteCallDelegate(Locker.InvokeUserCode_RpcPlayDenied__Byte));
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteUShort(this.OpenedChambers);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteUShort(this.OpenedChambers);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<ushort>(ref this.OpenedChambers, null, reader.ReadUShort());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<ushort>(ref this.OpenedChambers, null, reader.ReadUShort());
			}
		}

		public LockerLoot[] Loot;

		public LockerChamber[] Chambers;

		[SyncVar]
		public ushort OpenedChambers;

		[SerializeField]
		private AudioClip _grantedBeep;

		[SerializeField]
		private AudioClip _deniedBeep;

		[Header("Leave 0 to fill all chambers")]
		public int MinChambersToFill;

		public int MaxChambersToFill;

		private ushort? _prevOpened;

		private bool _serverChambersFilled;
	}
}
