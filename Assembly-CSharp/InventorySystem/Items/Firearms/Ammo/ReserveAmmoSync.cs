using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Firearms.Ammo;

public static class ReserveAmmoSync
{
	private class LastSent
	{
		public ItemType AmmoType;

		public int AmmoCount;
	}

	public readonly struct ReserveAmmoMessage : NetworkMessage
	{
		private readonly sbyte _ammoType;

		private readonly byte _amount;

		private readonly RecyclablePlayerId _player;

		public ReserveAmmoMessage(NetworkReader reader)
		{
			this._ammoType = reader.ReadSByte();
			this._amount = reader.ReadByte();
			this._player = reader.ReadRecyclablePlayerId();
		}

		public ReserveAmmoMessage(ReferenceHub owner, ItemType ammoType)
		{
			this._ammoType = (sbyte)ammoType;
			this._amount = (byte)Mathf.Clamp(owner.inventory.GetCurAmmo(ammoType), 0, 255);
			this._player = new RecyclablePlayerId(owner);
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteSByte(this._ammoType);
			writer.WriteByte(this._amount);
			writer.WriteRecyclablePlayerId(this._player);
		}

		public void Apply()
		{
			if (ReferenceHub.TryGetHub(this._player.Value, out var hub))
			{
				ReserveAmmoSync.Set(hub, (ItemType)this._ammoType, this._amount);
			}
		}
	}

	private static readonly Dictionary<ReferenceHub, Dictionary<ItemType, int>> SyncData = new Dictionary<ReferenceHub, Dictionary<ItemType, int>>();

	private static readonly Dictionary<ReferenceHub, LastSent> ServerLastSent = new Dictionary<ReferenceHub, LastSent>();

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			ReserveAmmoSync.SyncData.Remove(hub);
		};
		PlayerRoleManager.OnServerRoleSet += delegate(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
		{
			if (!newRole.IsAlive())
			{
				ReserveAmmoSync.SendAllToNewSpectator(hub);
			}
		};
		CustomNetworkManager.OnClientReady += delegate
		{
			ReserveAmmoSync.SyncData.Clear();
			NetworkClient.ReplaceHandler(delegate(ReserveAmmoMessage msg)
			{
				msg.Apply();
			});
		};
		StaticUnityMethods.OnUpdate += delegate
		{
			if (NetworkServer.active)
			{
				ReserveAmmoSync.UpdateDelta();
			}
		};
	}

	private static void SendAllToNewSpectator(ReferenceHub spectator)
	{
		foreach (AutosyncItem instance in AutosyncItem.Instances)
		{
			if (ReserveAmmoSync.TryUnpack(instance, out var owner, out var ammoType))
			{
				spectator.connectionToClient.Send(new ReserveAmmoMessage(owner, ammoType));
			}
		}
	}

	private static void UpdateDelta()
	{
		foreach (AutosyncItem instance in AutosyncItem.Instances)
		{
			if (!ReserveAmmoSync.TryUnpack(instance, out var owner, out var ammoType))
			{
				continue;
			}
			int curAmmo = owner.inventory.GetCurAmmo(ammoType);
			LastSent orAdd = ReserveAmmoSync.ServerLastSent.GetOrAdd(owner, () => new LastSent());
			if (orAdd.AmmoCount != curAmmo || orAdd.AmmoType != ammoType)
			{
				orAdd.AmmoType = ammoType;
				orAdd.AmmoCount = curAmmo;
				new ReserveAmmoMessage(owner, ammoType).SendToHubsConditionally((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole);
			}
		}
	}

	private static bool TryUnpack(AutosyncItem src, out ReferenceHub owner, out ItemType ammoType)
	{
		owner = null;
		ammoType = ItemType.KeycardJanitor;
		if (!(src is Firearm { HasOwner: not false } firearm))
		{
			return false;
		}
		if (!firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
		{
			return false;
		}
		ammoType = module.AmmoType;
		owner = firearm.Owner;
		return true;
	}

	private static void Set(ReferenceHub hub, ItemType ammoType, int reserveAmmo)
	{
		ReserveAmmoSync.SyncData.GetOrAdd(hub, () => new Dictionary<ItemType, int>())[ammoType] = reserveAmmo;
	}

	public static bool TryGet(ReferenceHub hub, ItemType ammoType, out int reserveAmmo)
	{
		if (NetworkServer.active || hub.isLocalPlayer)
		{
			reserveAmmo = hub.inventory.GetCurAmmo(ammoType);
			return true;
		}
		if (!ReserveAmmoSync.SyncData.TryGetValue(hub, out var value))
		{
			reserveAmmo = 0;
			return false;
		}
		return value.TryGetValue(ammoType, out reserveAmmo);
	}

	public static void WriteReserveAmmoMessage(this NetworkWriter writer, ReserveAmmoMessage value)
	{
		value.Serialize(writer);
	}

	public static ReserveAmmoMessage ReadReserveAmmoMessage(this NetworkReader reader)
	{
		return new ReserveAmmoMessage(reader);
	}
}
