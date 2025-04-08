using System;
using System.Collections.Generic;
using AudioPooling;
using CustomPlayerEffects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables
{
	public static class UsableItemsController
	{
		public static event Action<ReferenceHub, UsableItem> ServerOnUsingCompleted;

		public static event Action<StatusMessage> OnClientStatusReceived;

		[RuntimeInitializeOnLoadMethod]
		private static void InitOnLoad()
		{
			CustomNetworkManager.OnClientReady += UsableItemsController.OnClientReady;
			PlayerRoleManager.OnRoleChanged += UsableItemsController.ResetPlayerOnRoleChange;
			StaticUnityMethods.OnUpdate += UsableItemsController.Update;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(delegate(ReferenceHub hub)
			{
				UsableItemsController.Handlers.Remove(hub);
			}));
			Inventory.OnLocalClientStarted += UsableItemsController.StartTimes.Clear;
			CustomNetworkManager.OnClientReady += UsableItemsController.CurrentlyPlayingSources.Clear;
		}

		public static void OnClientReady()
		{
			NetworkServer.ReplaceHandler<StatusMessage>(new Action<NetworkConnectionToClient, StatusMessage>(UsableItemsController.ServerReceivedStatus), true);
			NetworkClient.ReplaceHandler<StatusMessage>(new Action<StatusMessage>(UsableItemsController.ClientReceivedStatus), true);
			NetworkClient.ReplaceHandler<ItemCooldownMessage>(new Action<ItemCooldownMessage>(UsableItemsController.ClientReceivedCooldown), true);
			UsableItemsController.GlobalItemCooldowns.Clear();
			UsableItemsController.Handlers.Clear();
		}

		public static PlayerHandler GetHandler(ReferenceHub ply)
		{
			PlayerHandler playerHandler;
			if (!UsableItemsController.Handlers.TryGetValue(ply, out playerHandler))
			{
				playerHandler = new PlayerHandler();
				UsableItemsController.Handlers.Add(ply, playerHandler);
			}
			return playerHandler;
		}

		public static float GetCooldown(ushort itemSerial, ItemBase item, PlayerHandler ply)
		{
			float num = 0f;
			float num2;
			if (UsableItemsController.GlobalItemCooldowns.TryGetValue(itemSerial, out num2))
			{
				num = num2;
			}
			float num3;
			if (ply.PersonalCooldowns.TryGetValue(item.ItemTypeId, out num3) && num3 > num)
			{
				num = num3;
			}
			return num - Time.timeSinceLevelLoad;
		}

		public static void PlaySoundOnPlayer(ReferenceHub ply, AudioClip clip)
		{
			ItemIdentifier curItem = ply.inventory.CurItem;
			PooledAudioSource pooledAudioSource = AudioSourcePoolManager.PlayOnTransform(clip, ply.transform, 15f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			pooledAudioSource.Source.pitch = curItem.TypeId.GetSpeedMultiplier(ply);
			UsableItemsController.CurrentlyPlayingSources[curItem.SerialNumber] = new AudioPoolSession(pooledAudioSource);
		}

		private static void Update()
		{
			if (!StaticUnityMethods.IsPlaying || !NetworkServer.active)
			{
				return;
			}
			foreach (KeyValuePair<ReferenceHub, PlayerHandler> keyValuePair in UsableItemsController.Handlers)
			{
				keyValuePair.Value.DoUpdate(keyValuePair.Key);
				CurrentlyUsedItem currentUsable = keyValuePair.Value.CurrentUsable;
				if (currentUsable.ItemSerial != 0)
				{
					float speedMultiplier = currentUsable.Item.ItemTypeId.GetSpeedMultiplier(keyValuePair.Key);
					if (currentUsable.ItemSerial != keyValuePair.Key.inventory.CurItem.SerialNumber)
					{
						if (currentUsable.Item != null)
						{
							currentUsable.Item.OnUsingCancelled();
						}
						keyValuePair.Value.CurrentUsable = CurrentlyUsedItem.None;
						keyValuePair.Key.inventory.connectionToClient.Send<StatusMessage>(new StatusMessage(StatusMessage.StatusType.Cancel, currentUsable.ItemSerial), 0);
					}
					else if (Time.timeSinceLevelLoad >= currentUsable.StartTime + currentUsable.Item.UseTime / speedMultiplier)
					{
						currentUsable.Item.ServerOnUsingCompleted();
						Action<ReferenceHub, UsableItem> serverOnUsingCompleted = UsableItemsController.ServerOnUsingCompleted;
						if (serverOnUsingCompleted != null)
						{
							serverOnUsingCompleted(keyValuePair.Key, currentUsable.Item);
						}
						keyValuePair.Value.CurrentUsable = CurrentlyUsedItem.None;
						PlayerEvents.OnUsedItem(new PlayerUsedItemEventArgs(keyValuePair.Key, currentUsable.Item));
					}
				}
			}
		}

		private static void ServerReceivedStatus(NetworkConnection conn, StatusMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHub(conn, out referenceHub))
			{
				return;
			}
			UsableItem usableItem = referenceHub.inventory.CurInstance as UsableItem;
			if (usableItem == null)
			{
				return;
			}
			if (usableItem.ItemSerial != msg.ItemSerial)
			{
				return;
			}
			PlayerHandler handler = UsableItemsController.GetHandler(referenceHub);
			StatusMessage.StatusType status = msg.Status;
			if (status != StatusMessage.StatusType.Start)
			{
				if (status != StatusMessage.StatusType.Cancel)
				{
					return;
				}
				if (!usableItem.ServerValidateCancelRequest(handler))
				{
					return;
				}
				if (handler.CurrentUsable.ItemSerial == 0)
				{
					return;
				}
				float speedMultiplier = handler.CurrentUsable.Item.ItemTypeId.GetSpeedMultiplier(referenceHub);
				if (handler.CurrentUsable.StartTime + handler.CurrentUsable.Item.MaxCancellableTime / speedMultiplier > Time.timeSinceLevelLoad)
				{
					PlayerCancellingUsingItemEventArgs playerCancellingUsingItemEventArgs = new PlayerCancellingUsingItemEventArgs(referenceHub, usableItem);
					PlayerEvents.OnCancellingUsingItem(playerCancellingUsingItemEventArgs);
					if (!playerCancellingUsingItemEventArgs.IsAllowed)
					{
						return;
					}
					handler.CurrentUsable.Item.OnUsingCancelled();
					handler.CurrentUsable = CurrentlyUsedItem.None;
					new StatusMessage(StatusMessage.StatusType.Cancel, msg.ItemSerial).SendToAuthenticated(0);
					PlayerEvents.OnCancelledUsingItem(new PlayerCancelledUsingItemEventArgs(referenceHub, usableItem));
				}
			}
			else
			{
				if (!usableItem.ServerValidateStartRequest(handler))
				{
					return;
				}
				if (handler.CurrentUsable.ItemSerial != 0)
				{
					return;
				}
				if (!usableItem.CanStartUsing)
				{
					return;
				}
				float cooldown = UsableItemsController.GetCooldown(msg.ItemSerial, usableItem, handler);
				if (cooldown > 0f)
				{
					conn.Send<ItemCooldownMessage>(new ItemCooldownMessage(msg.ItemSerial, cooldown), 0);
					return;
				}
				if (usableItem.ItemTypeId.GetSpeedMultiplier(referenceHub) > 0f)
				{
					PlayerUsingItemEventArgs playerUsingItemEventArgs = new PlayerUsingItemEventArgs(referenceHub, usableItem);
					PlayerEvents.OnUsingItem(playerUsingItemEventArgs);
					if (!playerUsingItemEventArgs.IsAllowed)
					{
						return;
					}
					handler.CurrentUsable = new CurrentlyUsedItem(usableItem, msg.ItemSerial, Time.timeSinceLevelLoad);
					handler.CurrentUsable.Item.OnUsingStarted();
					new StatusMessage(StatusMessage.StatusType.Start, msg.ItemSerial).SendToAuthenticated(0);
					return;
				}
			}
		}

		private static void ClientReceivedStatus(StatusMessage msg)
		{
			ushort itemSerial = msg.ItemSerial;
			bool flag = msg.Status == StatusMessage.StatusType.Start;
			AudioPoolSession audioPoolSession;
			if (!flag && UsableItemsController.CurrentlyPlayingSources.TryGetValue(itemSerial, out audioPoolSession) && audioPoolSession.SameSession)
			{
				audioPoolSession.HandledInstance.Source.Stop();
				UsableItemsController.StartTimes.Remove(itemSerial);
				UsableItemsController.CurrentlyPlayingSources.Remove(itemSerial);
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				AudioClip audioClip;
				if (referenceHub.isLocalPlayer)
				{
					ItemBase itemBase;
					if (!referenceHub.inventory.UserInventory.Items.TryGetValue(itemSerial, out itemBase))
					{
						continue;
					}
					UsableItem usableItem = itemBase as UsableItem;
					if (usableItem == null)
					{
						continue;
					}
					if (flag)
					{
						usableItem.OnUsingStarted();
					}
					else
					{
						usableItem.OnUsingCancelled();
					}
					if (!(usableItem.UsingSfxClip != null))
					{
						break;
					}
					audioClip = usableItem.UsingSfxClip;
				}
				else
				{
					if (referenceHub.inventory.CurItem.SerialNumber != itemSerial)
					{
						continue;
					}
					ItemBase itemBase2;
					if (!InventoryItemLoader.AvailableItems.TryGetValue(referenceHub.inventory.CurItem.TypeId, out itemBase2))
					{
						break;
					}
					UsableItem usableItem2 = itemBase2 as UsableItem;
					if (usableItem2 == null || !(usableItem2.UsingSfxClip != null))
					{
						break;
					}
					audioClip = usableItem2.UsingSfxClip;
				}
				if (!flag)
				{
					break;
				}
				UsableItemsController.PlaySoundOnPlayer(referenceHub, audioClip);
				UsableItemsController.StartTimes[itemSerial] = Time.timeSinceLevelLoad;
				break;
			}
			Action<StatusMessage> onClientStatusReceived = UsableItemsController.OnClientStatusReceived;
			if (onClientStatusReceived == null)
			{
				return;
			}
			onClientStatusReceived(msg);
		}

		private static void ClientReceivedCooldown(ItemCooldownMessage msg)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			ItemBase itemBase;
			if (!referenceHub.inventory.UserInventory.Items.TryGetValue(msg.ItemSerial, out itemBase))
			{
				return;
			}
			UsableItem usableItem = itemBase as UsableItem;
			if (usableItem == null)
			{
				return;
			}
			usableItem.RemainingCooldown = msg.RemainingTime;
		}

		private static void ResetPlayerOnRoleChange(ReferenceHub ply, PlayerRoleBase r1, PlayerRoleBase r2)
		{
			UsableItemsController.GetHandler(ply).ResetAll();
		}

		public static readonly Dictionary<ReferenceHub, PlayerHandler> Handlers = new Dictionary<ReferenceHub, PlayerHandler>();

		public static readonly Dictionary<ushort, float> GlobalItemCooldowns = new Dictionary<ushort, float>();

		public static readonly Dictionary<ushort, float> StartTimes = new Dictionary<ushort, float>();

		private static readonly Dictionary<ushort, AudioPoolSession> CurrentlyPlayingSources = new Dictionary<ushort, AudioPoolSession>();
	}
}
