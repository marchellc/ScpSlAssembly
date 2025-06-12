using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MapGeneration.StaticHelpers;
using Mirror;
using Mirror.RemoteCalls;
using NorthwoodLib.Pools;
using UnityEngine;

namespace MapGeneration.Distributors;

public class Locker : SpawnableStructure, IServerInteractable, IInteractable, IBlockStaticBatching
{
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

	private float _deniedCooldown;

	private const float DeniedCooldownDuration = 1f;

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public ushort NetworkOpenedChambers
	{
		get
		{
			return this.OpenedChambers;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.OpenedChambers, 1uL, null);
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!this.Chambers.TryGet(colliderId, out var element) || !element.CanInteract)
		{
			return;
		}
		bool flag = !this.CheckTogglePerms(colliderId, ply, out var callback);
		PlayerInteractingLockerEventArgs e = new PlayerInteractingLockerEventArgs(ply, this, this.Chambers[colliderId], !flag);
		PlayerEvents.OnInteractingLocker(e);
		if (!e.IsAllowed)
		{
			return;
		}
		flag = !e.CanOpen;
		if (flag)
		{
			if (this._deniedCooldown <= 0f)
			{
				this.RpcPlayDenied(colliderId, ply.GetCombinedPermissions(element));
				callback?.Invoke(element, success: false);
				this._deniedCooldown = 1f;
			}
			PlayerEvents.OnInteractedLocker(new PlayerInteractedLockerEventArgs(ply, this, this.Chambers[colliderId], !flag));
		}
		else
		{
			element.SetDoor(!element.IsOpen, this._grantedBeep);
			this.RefreshOpenedSyncvar();
			callback?.Invoke(element, success: true);
			PlayerEvents.OnInteractedLocker(new PlayerInteractedLockerEventArgs(ply, this, this.Chambers[colliderId], !flag));
		}
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
		if (num2 != this.OpenedChambers)
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
			int num = list[Random.Range(0, list.Count)];
			LockerLoot lockerLoot2 = this.Loot[num];
			ch.SpawnItem(lockerLoot2.TargetItem, Random.Range(lockerLoot2.MinPerChamber, lockerLoot2.MaxPerChamber + 1));
			lockerLoot2.RemainingUses--;
		}
		ListPool<int>.Shared.Return(list);
	}

	[ClientRpc]
	public void RpcPlayDenied(byte chamberId, DoorPermissionFlags perms)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		NetworkWriterExtensions.WriteByte(writer, chamberId);
		GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(writer, perms);
		this.SendRPCInternal("System.Void MapGeneration.Distributors.Locker::RpcPlayDenied(System.Byte,Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", 1380298176, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	protected virtual void Update()
	{
		if (!NetworkClient.ready)
		{
			return;
		}
		if (NetworkServer.active)
		{
			if (this._deniedCooldown > 0f)
			{
				this._deniedCooldown -= Time.deltaTime;
			}
			if (!this._serverChambersFilled)
			{
				this.ServerFillChambers();
				this._serverChambersFilled = true;
			}
		}
		if (this._prevOpened != this.OpenedChambers)
		{
			int num = 1;
			LockerChamber[] chambers = this.Chambers;
			foreach (LockerChamber lockerChamber in chambers)
			{
				lockerChamber.SetDoor((this.OpenedChambers & num) == num || !lockerChamber.AnimatorSet, this._grantedBeep);
				num *= 2;
			}
			this._prevOpened = this.OpenedChambers;
		}
	}

	protected virtual void ServerFillChambers()
	{
		List<LockerChamber> list = new List<LockerChamber>(this.Chambers);
		if (this.MinChambersToFill != 0 && this.MaxChambersToFill >= this.MinChambersToFill)
		{
			int num = this.Chambers.Length - Random.Range(this.MinChambersToFill, this.MaxChambersToFill + 1);
			for (int i = 0; i < num; i++)
			{
				list.RemoveAt(Random.Range(0, list.Count));
			}
		}
		foreach (LockerChamber item in list)
		{
			this.FillChamber(item);
		}
	}

	protected virtual bool CheckTogglePerms(int chamberId, ReferenceHub ply, out PermissionUsed callback)
	{
		return this.Chambers[chamberId].CheckPermissions(ply, out callback);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcPlayDenied__Byte__DoorPermissionFlags(byte chamberId, DoorPermissionFlags perms)
	{
		if (chamberId <= this.Chambers.Length)
		{
			this.Chambers[chamberId].PlayDenied(this._deniedBeep, perms, 1f);
		}
	}

	protected static void InvokeUserCode_RpcPlayDenied__Byte__DoorPermissionFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayDenied called on server.");
		}
		else
		{
			((Locker)obj).UserCode_RpcPlayDenied__Byte__DoorPermissionFlags(NetworkReaderExtensions.ReadByte(reader), GeneratedNetworkCode._Read_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(reader));
		}
	}

	static Locker()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(Locker), "System.Void MapGeneration.Distributors.Locker::RpcPlayDenied(System.Byte,Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", InvokeUserCode_RpcPlayDenied__Byte__DoorPermissionFlags);
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
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteUShort(this.OpenedChambers);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.OpenedChambers, null, reader.ReadUShort());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.OpenedChambers, null, reader.ReadUShort());
		}
	}
}
