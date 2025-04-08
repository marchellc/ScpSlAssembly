using System;
using System.Collections.Generic;
using InventorySystem.Items;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace InventorySystem.Disarming
{
	public static class DisarmedPlayers
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StaticUnityMethods.OnUpdate += DisarmedPlayers.Update;
			PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				if (!(prevRole is IInventoryRole))
				{
					return;
				}
				for (int i = 0; i < DisarmedPlayers.Entries.Count; i++)
				{
					if (DisarmedPlayers.Entries[i].DisarmedPlayer == hub.netId)
					{
						DisarmedPlayers.Entries.RemoveAt(i);
						new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated(0);
						return;
					}
				}
			};
			Inventory.OnItemsModified += delegate(ReferenceHub hub)
			{
				if (!NetworkServer.active)
				{
					return;
				}
				if (hub.inventory.UserInventory.Items.Any(delegate(KeyValuePair<ushort, ItemBase> item)
				{
					ItemCategory category = item.Value.Category;
					return category == ItemCategory.Firearm || category == ItemCategory.SpecialWeapon;
				}))
				{
					return;
				}
				for (int j = 0; j < DisarmedPlayers.Entries.Count; j++)
				{
					if (DisarmedPlayers.Entries[j].Disarmer == hub.netId)
					{
						DisarmedPlayers.Entries.RemoveAt(j);
						new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated(0);
						return;
					}
				}
			};
		}

		private static void Update()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			for (int i = 0; i < DisarmedPlayers.Entries.Count; i++)
			{
				if (!DisarmedPlayers.ValidateEntry(DisarmedPlayers.Entries[i]))
				{
					DisarmedPlayers.Entries.RemoveAt(i);
					new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated(0);
					return;
				}
			}
		}

		private static bool ValidateEntry(DisarmedPlayers.DisarmedEntry entry)
		{
			if (entry.Disarmer == 0U)
			{
				return true;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(entry.DisarmedPlayer, out referenceHub))
			{
				return false;
			}
			ReferenceHub referenceHub2;
			if (!ReferenceHub.TryGetHubNetID(entry.Disarmer, out referenceHub2))
			{
				return false;
			}
			if (!referenceHub2.ValidateDisarmament(referenceHub))
			{
				return false;
			}
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			IFpcRole fpcRole2 = referenceHub2.roleManager.CurrentRole as IFpcRole;
			if (fpcRole2 == null)
			{
				return false;
			}
			Vector3 position = fpcRole.FpcModule.Position;
			Vector3 position2 = fpcRole2.FpcModule.Position;
			if ((position - position2).sqrMagnitude > 8100f)
			{
				return false;
			}
			referenceHub.inventory.ServerDropEverything();
			return true;
		}

		public static bool IsDisarmed(this Inventory inv)
		{
			using (List<DisarmedPlayers.DisarmedEntry>.Enumerator enumerator = DisarmedPlayers.Entries.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.DisarmedPlayer == inv.netId)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static void SetDisarmedStatus(this Inventory inv, Inventory disarmer)
		{
			bool flag;
			do
			{
				flag = true;
				for (int i = 0; i < DisarmedPlayers.Entries.Count; i++)
				{
					if (DisarmedPlayers.Entries[i].DisarmedPlayer == inv.netId)
					{
						DisarmedPlayers.Entries.RemoveAt(i);
						flag = false;
						break;
					}
				}
			}
			while (!flag);
			if (disarmer != null)
			{
				DisarmedPlayers.Entries.Add(new DisarmedPlayers.DisarmedEntry(inv.netId, disarmer.netId));
			}
		}

		public static bool ValidateDisarmament(this ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			IInventoryRole inventoryRole = targetHub.roleManager.CurrentRole as IInventoryRole;
			return inventoryRole != null && inventoryRole.AllowDisarming(disarmerHub);
		}

		public static bool CanStartDisarming(this ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			if (!disarmerHub.ValidateDisarmament(targetHub))
			{
				return false;
			}
			ItemBase curInstance = disarmerHub.inventory.CurInstance;
			if (curInstance != null)
			{
				IDisarmingItem disarmingItem = curInstance as IDisarmingItem;
				if (disarmingItem != null)
				{
					return disarmingItem.AllowDisarming;
				}
			}
			return false;
		}

		public static bool CanUndisarm(this ReferenceHub disarmerHub, ReferenceHub targetHub)
		{
			if (!targetHub.inventory.IsDisarmed())
			{
				return false;
			}
			IInventoryRole inventoryRole = targetHub.roleManager.CurrentRole as IInventoryRole;
			return inventoryRole == null || inventoryRole.AllowUndisarming(disarmerHub);
		}

		public static List<DisarmedPlayers.DisarmedEntry> Entries = new List<DisarmedPlayers.DisarmedEntry>();

		private const float AutoDisarmDistanceSquared = 8100f;

		public readonly struct DisarmedEntry
		{
			public DisarmedEntry(uint disarmedPlayer, uint disarmer)
			{
				this.DisarmedPlayer = disarmedPlayer;
				this.Disarmer = disarmer;
			}

			public readonly uint DisarmedPlayer;

			public readonly uint Disarmer;
		}
	}
}
