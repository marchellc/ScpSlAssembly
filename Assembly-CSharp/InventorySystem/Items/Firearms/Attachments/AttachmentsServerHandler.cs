using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Attachments
{
	public static class AttachmentsServerHandler
	{
		public static uint ServerGetReceivedPlayerPreference(Firearm firearmInstance)
		{
			ReferenceHub owner = firearmInstance.Owner;
			Dictionary<ItemType, uint> dictionary;
			if (!AttachmentsServerHandler.PlayerPreferences.TryGetValue(owner, out dictionary))
			{
				return 0U;
			}
			uint num;
			dictionary.TryGetValue(firearmInstance.ItemTypeId, out num);
			return num;
		}

		public static bool AnyWorkstationsNearby(ReferenceHub ply)
		{
			return WorkstationController.AllWorkstations.Any((WorkstationController workstation) => !(workstation == null) && workstation.Status == 3 && workstation.IsInRange(ply));
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Inventory.OnServerStarted += AttachmentsServerHandler.OnServerStarted;
		}

		private static void OnServerStarted()
		{
			AttachmentsServerHandler.PlayerPreferences.Clear();
			NetworkServer.ReplaceHandler<AttachmentsSetupPreference>(new Action<NetworkConnectionToClient, AttachmentsSetupPreference>(AttachmentsServerHandler.ServerReceivePreference), true);
			NetworkServer.ReplaceHandler<AttachmentsChangeRequest>(new Action<NetworkConnectionToClient, AttachmentsChangeRequest>(AttachmentsServerHandler.ServerReceiveChangeRequest), true);
		}

		private static void ServerReceiveChangeRequest(NetworkConnection conn, AttachmentsChangeRequest msg)
		{
			ReferenceHub referenceHub;
			if (!NetworkServer.active || !ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			Firearm firearm = referenceHub.inventory.CurInstance as Firearm;
			if (firearm == null || firearm == null)
			{
				return;
			}
			if (!AttachmentsServerHandler.AnyWorkstationsNearby(referenceHub))
			{
				return;
			}
			firearm.ApplyAttachmentsCode(msg.AttachmentsCode, true);
			firearm.ServerResendAttachmentCode();
			AttachmentsServerHandler.ServerApplyPreference(referenceHub, firearm.ItemTypeId, msg.AttachmentsCode);
		}

		private static void ServerReceivePreference(NetworkConnection conn, AttachmentsSetupPreference msg)
		{
			ReferenceHub referenceHub;
			if (!NetworkServer.active || !ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			AttachmentsServerHandler.ServerApplyPreference(referenceHub, msg.Weapon, msg.AttachmentsCode);
		}

		private static void ServerApplyPreference(ReferenceHub hub, ItemType weapon, uint attCode)
		{
			AttachmentsServerHandler.PlayerPreferences.GetOrAdd(hub, () => new Dictionary<ItemType, uint>())[weapon] = attCode;
		}

		public static readonly Dictionary<ReferenceHub, Dictionary<ItemType, uint>> PlayerPreferences = new Dictionary<ReferenceHub, Dictionary<ItemType, uint>>();
	}
}
