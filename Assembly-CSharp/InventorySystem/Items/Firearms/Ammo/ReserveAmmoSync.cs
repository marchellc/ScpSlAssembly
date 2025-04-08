using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using PlayerRoles;
using PlayerRoles.Spectating;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Firearms.Ammo
{
	public static class ReserveAmmoSync
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				ReserveAmmoSync.SyncData.Remove(hub);
			}));
			PlayerRoleManager.OnServerRoleSet += delegate(ReferenceHub hub, RoleTypeId newRole, RoleChangeReason reason)
			{
				if (newRole.IsAlive())
				{
					return;
				}
				ReserveAmmoSync.SendAllToNewSpectator(hub);
			};
			CustomNetworkManager.OnClientReady += delegate
			{
				ReserveAmmoSync.SyncData.Clear();
				NetworkClient.ReplaceHandler<ReserveAmmoSync.ReserveAmmoMessage>(delegate(ReserveAmmoSync.ReserveAmmoMessage msg)
				{
					msg.Apply();
				}, true);
			};
			StaticUnityMethods.OnUpdate += delegate
			{
				if (!NetworkServer.active)
				{
					return;
				}
				ReserveAmmoSync.UpdateDelta();
			};
		}

		private static void SendAllToNewSpectator(ReferenceHub spectator)
		{
			using (HashSet<AutosyncItem>.Enumerator enumerator = AutosyncItem.Instances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ReferenceHub referenceHub;
					ItemType itemType;
					if (ReserveAmmoSync.TryUnpack(enumerator.Current, out referenceHub, out itemType))
					{
						spectator.connectionToClient.Send<ReserveAmmoSync.ReserveAmmoMessage>(new ReserveAmmoSync.ReserveAmmoMessage(referenceHub, itemType), 0);
					}
				}
			}
		}

		private static void UpdateDelta()
		{
			using (HashSet<AutosyncItem>.Enumerator enumerator = AutosyncItem.Instances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ReferenceHub referenceHub;
					ItemType itemType;
					if (ReserveAmmoSync.TryUnpack(enumerator.Current, out referenceHub, out itemType))
					{
						int curAmmo = (int)referenceHub.inventory.GetCurAmmo(itemType);
						ReserveAmmoSync.LastSent orAdd = ReserveAmmoSync.ServerLastSent.GetOrAdd(referenceHub, () => new ReserveAmmoSync.LastSent());
						if (orAdd.AmmoCount != curAmmo || orAdd.AmmoType != itemType)
						{
							orAdd.AmmoType = itemType;
							orAdd.AmmoCount = curAmmo;
							new ReserveAmmoSync.ReserveAmmoMessage(referenceHub, itemType).SendToHubsConditionally((ReferenceHub x) => x.roleManager.CurrentRole is SpectatorRole, 0);
						}
					}
				}
			}
		}

		private static bool TryUnpack(AutosyncItem src, out ReferenceHub owner, out ItemType ammoType)
		{
			owner = null;
			ammoType = ItemType.KeycardJanitor;
			Firearm firearm = src as Firearm;
			if (firearm == null || !firearm.HasOwner)
			{
				return false;
			}
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			if (!firearm.TryGetModule(out primaryAmmoContainerModule, true))
			{
				return false;
			}
			ammoType = primaryAmmoContainerModule.AmmoType;
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
				reserveAmmo = (int)hub.inventory.GetCurAmmo(ammoType);
				return true;
			}
			Dictionary<ItemType, int> dictionary;
			if (!ReserveAmmoSync.SyncData.TryGetValue(hub, out dictionary))
			{
				reserveAmmo = 0;
				return false;
			}
			return dictionary.TryGetValue(ammoType, out reserveAmmo);
		}

		public static void WriteReserveAmmoMessage(this NetworkWriter writer, ReserveAmmoSync.ReserveAmmoMessage value)
		{
			value.Serialize(writer);
		}

		public static ReserveAmmoSync.ReserveAmmoMessage ReadReserveAmmoMessage(this NetworkReader reader)
		{
			return new ReserveAmmoSync.ReserveAmmoMessage(reader);
		}

		private static readonly Dictionary<ReferenceHub, Dictionary<ItemType, int>> SyncData = new Dictionary<ReferenceHub, Dictionary<ItemType, int>>();

		private static readonly Dictionary<ReferenceHub, ReserveAmmoSync.LastSent> ServerLastSent = new Dictionary<ReferenceHub, ReserveAmmoSync.LastSent>();

		private class LastSent
		{
			public ItemType AmmoType;

			public int AmmoCount;
		}

		public readonly struct ReserveAmmoMessage : NetworkMessage
		{
			public ReserveAmmoMessage(NetworkReader reader)
			{
				this._ammoType = reader.ReadSByte();
				this._amount = reader.ReadByte();
				this._player = reader.ReadRecyclablePlayerId();
			}

			public ReserveAmmoMessage(ReferenceHub owner, ItemType ammoType)
			{
				this._ammoType = (sbyte)ammoType;
				this._amount = (byte)Mathf.Clamp((int)owner.inventory.GetCurAmmo(ammoType), 0, 255);
				this._player = new RecyclablePlayerId(owner.PlayerId);
			}

			public void Serialize(NetworkWriter writer)
			{
				writer.WriteSByte(this._ammoType);
				writer.WriteByte(this._amount);
				writer.WriteRecyclablePlayerId(this._player);
			}

			public void Apply()
			{
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetHub(this._player.Value, out referenceHub))
				{
					return;
				}
				ReserveAmmoSync.Set(referenceHub, (ItemType)this._ammoType, (int)this._amount);
			}

			private readonly sbyte _ammoType;

			private readonly byte _amount;

			private readonly RecyclablePlayerId _player;
		}
	}
}
