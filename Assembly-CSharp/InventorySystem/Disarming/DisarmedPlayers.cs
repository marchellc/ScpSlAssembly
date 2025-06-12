using System.Collections.Generic;
using InventorySystem.Items;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.Networking;
using Utils.NonAllocLINQ;

namespace InventorySystem.Disarming;

public static class DisarmedPlayers
{
	public readonly struct DisarmedEntry
	{
		public readonly uint DisarmedPlayer;

		public readonly uint Disarmer;

		public DisarmedEntry(uint disarmedPlayer, uint disarmer)
		{
			this.DisarmedPlayer = disarmedPlayer;
			this.Disarmer = disarmer;
		}
	}

	public static List<DisarmedEntry> Entries = new List<DisarmedEntry>();

	private const float AutoDisarmDistanceSquared = 8100f;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnUpdate += Update;
		PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (NetworkServer.active && prevRole is IInventoryRole)
			{
				for (int i = 0; i < DisarmedPlayers.Entries.Count; i++)
				{
					if (DisarmedPlayers.Entries[i].DisarmedPlayer == hub.netId)
					{
						DisarmedPlayers.Entries.RemoveAt(i);
						new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated();
						break;
					}
				}
			}
		};
		Inventory.OnItemsModified += delegate(ReferenceHub hub)
		{
			if (NetworkServer.active && !hub.inventory.UserInventory.Items.Any(delegate(KeyValuePair<ushort, ItemBase> item)
			{
				ItemCategory category = item.Value.Category;
				return category == ItemCategory.Firearm || category == ItemCategory.SpecialWeapon;
			}))
			{
				for (int num = 0; num < DisarmedPlayers.Entries.Count; num++)
				{
					if (DisarmedPlayers.Entries[num].Disarmer == hub.netId)
					{
						DisarmedPlayers.Entries.RemoveAt(num);
						new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated();
						break;
					}
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
				new DisarmedPlayersListMessage(DisarmedPlayers.Entries).SendToAuthenticated();
				break;
			}
		}
	}

	private static bool ValidateEntry(DisarmedEntry entry)
	{
		if (entry.Disarmer == 0)
		{
			return true;
		}
		if (!ReferenceHub.TryGetHubNetID(entry.DisarmedPlayer, out var hub))
		{
			return false;
		}
		if (!ReferenceHub.TryGetHubNetID(entry.Disarmer, out var hub2))
		{
			return false;
		}
		if (!hub2.ValidateDisarmament(hub))
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is IFpcRole fpcRole))
		{
			return false;
		}
		if (!(hub2.roleManager.CurrentRole is IFpcRole fpcRole2))
		{
			return false;
		}
		Vector3 position = fpcRole.FpcModule.Position;
		Vector3 position2 = fpcRole2.FpcModule.Position;
		if ((position - position2).sqrMagnitude > 8100f)
		{
			return false;
		}
		hub.inventory.ServerDropEverything();
		return true;
	}

	public static bool IsDisarmed(this Inventory inv)
	{
		foreach (DisarmedEntry entry in DisarmedPlayers.Entries)
		{
			if (entry.DisarmedPlayer == inv.netId)
			{
				return true;
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
			DisarmedPlayers.Entries.Add(new DisarmedEntry(inv.netId, disarmer.netId));
		}
	}

	public static bool ValidateDisarmament(this ReferenceHub disarmerHub, ReferenceHub targetHub)
	{
		if (targetHub.roleManager.CurrentRole is IInventoryRole inventoryRole)
		{
			return inventoryRole.AllowDisarming(disarmerHub);
		}
		return false;
	}

	public static bool CanStartDisarming(this ReferenceHub disarmerHub, ReferenceHub targetHub)
	{
		if (!disarmerHub.ValidateDisarmament(targetHub))
		{
			return false;
		}
		ItemBase curInstance = disarmerHub.inventory.CurInstance;
		if (curInstance != null && curInstance is IDisarmingItem disarmingItem)
		{
			return disarmingItem.AllowDisarming;
		}
		return false;
	}

	public static bool CanUndisarm(this ReferenceHub disarmerHub, ReferenceHub targetHub)
	{
		if (!targetHub.inventory.IsDisarmed())
		{
			return false;
		}
		if (targetHub.roleManager.CurrentRole is IInventoryRole inventoryRole)
		{
			return inventoryRole.AllowUndisarming(disarmerHub);
		}
		return true;
	}
}
