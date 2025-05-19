using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Attachments;

public static class AttachmentsServerHandler
{
	public static readonly Dictionary<ReferenceHub, Dictionary<ItemType, uint>> PlayerPreferences = new Dictionary<ReferenceHub, Dictionary<ItemType, uint>>();

	public static uint ServerGetReceivedPlayerPreference(Firearm firearmInstance)
	{
		ReferenceHub owner = firearmInstance.Owner;
		if (!PlayerPreferences.TryGetValue(owner, out var value))
		{
			return 0u;
		}
		value.TryGetValue(firearmInstance.ItemTypeId, out var value2);
		return value2;
	}

	public static bool AnyWorkstationsNearby(ReferenceHub ply)
	{
		return WorkstationController.AllWorkstations.Any(delegate(WorkstationController workstation)
		{
			if (workstation == null)
			{
				return false;
			}
			if (workstation.Status != 3)
			{
				return false;
			}
			return workstation.IsInRange(ply) ? true : false;
		});
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Inventory.OnServerStarted += OnServerStarted;
	}

	private static void OnServerStarted()
	{
		PlayerPreferences.Clear();
		NetworkServer.ReplaceHandler<AttachmentsSetupPreference>(ServerReceivePreference);
		NetworkServer.ReplaceHandler<AttachmentsChangeRequest>(ServerReceiveChangeRequest);
	}

	private static void ServerReceiveChangeRequest(NetworkConnection conn, AttachmentsChangeRequest msg)
	{
		if (NetworkServer.active && ReferenceHub.TryGetHub(conn, out var hub) && hub.inventory.CurInstance is Firearm firearm && !(firearm == null) && AnyWorkstationsNearby(hub))
		{
			firearm.ApplyAttachmentsCode(msg.AttachmentsCode, reValidate: true);
			firearm.ServerResendAttachmentCode();
			ServerApplyPreference(hub, firearm.ItemTypeId, msg.AttachmentsCode);
		}
	}

	private static void ServerReceivePreference(NetworkConnection conn, AttachmentsSetupPreference msg)
	{
		if (NetworkServer.active && ReferenceHub.TryGetHub(conn, out var hub))
		{
			ServerApplyPreference(hub, msg.Weapon, msg.AttachmentsCode);
		}
	}

	private static void ServerApplyPreference(ReferenceHub hub, ItemType weapon, uint attCode)
	{
		PlayerPreferences.GetOrAdd(hub, () => new Dictionary<ItemType, uint>())[weapon] = attCode;
	}
}
