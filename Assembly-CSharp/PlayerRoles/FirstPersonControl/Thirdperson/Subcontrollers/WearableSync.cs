using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public static class WearableSync
	{
		public static WearableElements GetWearables(ReferenceHub hub)
		{
			return WearableSync.Database.GetValueOrDefault(hub, WearableElements.None);
		}

		public static void OverrideWearables(this ReferenceHub hub, WearableElements newWearables)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Unable to set wearables on client!");
			}
			WearableSync.Database[hub] = newWearables;
			NetworkServer.SendToAll<WearableSync.WearableSyncMessage>(new WearableSync.WearableSyncMessage
			{
				HubNetId = hub.netId,
				Data = newWearables
			}, 0, true);
		}

		public static void EnableWearables(this ReferenceHub hub, WearableElements toEnable)
		{
			WearableElements wearables = WearableSync.GetWearables(hub);
			WearableElements wearableElements = wearables | toEnable;
			if (wearableElements != wearables)
			{
				hub.OverrideWearables(wearableElements);
			}
		}

		public static void DisableWearables(this ReferenceHub hub, WearableElements toDisable)
		{
			WearableElements wearables = WearableSync.GetWearables(hub);
			WearableElements wearableElements = wearables & ~toDisable;
			if (wearableElements != wearables)
			{
				hub.OverrideWearables(wearableElements);
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(WearableSync.OnHubAdded));
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(WearableSync.OnHubRemoved));
			PlayerRoleManager.OnServerRoleSet += WearableSync.OnServerRoleSet;
			CustomNetworkManager.OnClientReady += WearableSync.OnClientReady;
		}

		private static void OnClientReady()
		{
			WearableSync.Database.Clear();
			NetworkClient.ReplaceHandler<WearableSync.WearableSyncMessage>(new Action<WearableSync.WearableSyncMessage>(WearableSync.ProcessMessage), true);
		}

		private static void ProcessMessage(WearableSync.WearableSyncMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(msg.HubNetId, out referenceHub))
			{
				return;
			}
			WearableSync.Database[referenceHub] = msg.Data;
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			AnimatedCharacterModel animatedCharacterModel = fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return;
			}
			WearableSubcontroller wearableSubcontroller;
			if (!animatedCharacterModel.TryGetSubcontroller<WearableSubcontroller>(out wearableSubcontroller))
			{
				return;
			}
			wearableSubcontroller.ClientReceiveWearables(msg.Data);
		}

		private static void OnHubRemoved(ReferenceHub hub)
		{
			WearableSync.Database.Remove(hub);
		}

		private static void OnHubAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (KeyValuePair<ReferenceHub, WearableElements> keyValuePair in WearableSync.Database)
			{
				if (keyValuePair.Value != WearableElements.None)
				{
					hub.connectionToClient.Send<WearableSync.WearableSyncMessage>(new WearableSync.WearableSyncMessage
					{
						HubNetId = keyValuePair.Key.netId,
						Data = keyValuePair.Value
					}, 0);
				}
			}
		}

		private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			userHub.OverrideWearables(WearableElements.None);
		}

		private static readonly Dictionary<ReferenceHub, WearableElements> Database = new Dictionary<ReferenceHub, WearableElements>();

		public struct WearableSyncMessage : NetworkMessage
		{
			public uint HubNetId;

			public WearableElements Data;
		}
	}
}
