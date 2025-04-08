using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public static class EmotionSync
	{
		public static EmotionPresetType GetEmotionPreset(ReferenceHub hub)
		{
			return EmotionSync.Database.GetValueOrDefault(hub, EmotionPresetType.Neutral);
		}

		public static void ServerSetEmotionPreset(this ReferenceHub hub, EmotionPresetType preset)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Unable to set emotions on client!");
			}
			EmotionSync.Database[hub] = preset;
			NetworkServer.SendToAll<EmotionSync.EmotionSyncMessage>(new EmotionSync.EmotionSyncMessage
			{
				HubNetId = hub.netId,
				Data = preset
			}, 0, true);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(EmotionSync.OnHubAdded));
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(EmotionSync.OnHubRemoved));
			PlayerRoleManager.OnServerRoleSet += EmotionSync.OnServerRoleSet;
			CustomNetworkManager.OnClientReady += EmotionSync.OnClientReady;
		}

		private static void OnClientReady()
		{
			EmotionSync.Database.Clear();
			NetworkClient.ReplaceHandler<EmotionSync.EmotionSyncMessage>(new Action<EmotionSync.EmotionSyncMessage>(EmotionSync.ProcessMessage), true);
			NetworkServer.ReplaceHandler<EmotionSync.EmotionSyncMessage>(new Action<NetworkConnectionToClient, EmotionSync.EmotionSyncMessage>(EmotionSync.TempServerProcessMessage), true);
		}

		private static void ProcessMessage(EmotionSync.EmotionSyncMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(msg.HubNetId, out referenceHub))
			{
				return;
			}
			EmotionSync.Database[referenceHub] = msg.Data;
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
			EmotionSubcontroller emotionSubcontroller;
			if (!animatedCharacterModel.TryGetSubcontroller<EmotionSubcontroller>(out emotionSubcontroller))
			{
				return;
			}
			emotionSubcontroller.SetPreset(msg.Data);
		}

		private static void OnHubRemoved(ReferenceHub hub)
		{
			EmotionSync.Database.Remove(hub);
		}

		private static void OnHubAdded(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (KeyValuePair<ReferenceHub, EmotionPresetType> keyValuePair in EmotionSync.Database)
			{
				if (keyValuePair.Value != EmotionPresetType.Neutral)
				{
					hub.connectionToClient.Send<EmotionSync.EmotionSyncMessage>(new EmotionSync.EmotionSyncMessage
					{
						HubNetId = keyValuePair.Key.netId,
						Data = keyValuePair.Value
					}, 0);
				}
			}
		}

		private static void OnServerRoleSet(ReferenceHub userHub, RoleTypeId newRole, RoleChangeReason reason)
		{
			userHub.ServerSetEmotionPreset(EmotionPresetType.Neutral);
		}

		private static void TempServerProcessMessage(NetworkConnection conn, EmotionSync.EmotionSyncMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(msg.HubNetId, out referenceHub) || referenceHub.netIdentity != conn.identity)
			{
				return;
			}
			referenceHub.ServerSetEmotionPreset(msg.Data);
		}

		private static readonly Dictionary<ReferenceHub, EmotionPresetType> Database = new Dictionary<ReferenceHub, EmotionPresetType>();

		public struct EmotionSyncMessage : NetworkMessage
		{
			public uint HubNetId;

			public EmotionPresetType Data;
		}
	}
}
