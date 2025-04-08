using System;
using System.Collections.Generic;
using CentralAuth;
using InventorySystem;
using Mirror;
using PlayerRoles.Visibility;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public static class FpcServerPositionDistributor
	{
		private static float SendRate
		{
			get
			{
				return 1f / (float)Mathf.Clamp((int)ServerStatic.ServerTickrate, 10, 60);
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StaticUnityMethods.OnLateUpdate += FpcServerPositionDistributor.LateUpdate;
			PlayerRoleManager.OnRoleChanged += FpcServerPositionDistributor.ResetPlayer;
			Inventory.OnServerStarted += FpcServerPositionDistributor.PreviouslySent.Clear;
			ReferenceHub.OnPlayerAdded = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerAdded, new Action<ReferenceHub>(delegate(ReferenceHub rh)
			{
				FpcServerPositionDistributor.EnsureArrayCapacity();
			}));
		}

		private static void ResetPlayer(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (!(prevRole is IFpcRole))
			{
				return;
			}
			uint netId = userHub.netId;
			foreach (KeyValuePair<uint, Dictionary<uint, FpcSyncData>> keyValuePair in FpcServerPositionDistributor.PreviouslySent)
			{
				keyValuePair.Value.Remove(netId);
			}
		}

		private static void LateUpdate()
		{
			if (!NetworkServer.active || !StaticUnityMethods.IsPlaying)
			{
				return;
			}
			FpcServerPositionDistributor._sendCooldown += Time.deltaTime;
			if (FpcServerPositionDistributor._sendCooldown < FpcServerPositionDistributor.SendRate)
			{
				return;
			}
			FpcServerPositionDistributor._sendCooldown -= FpcServerPositionDistributor.SendRate;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.Mode != ClientInstanceMode.Unverified && !referenceHub.isLocalPlayer)
				{
					referenceHub.connectionToClient.Send<FpcPositionMessage>(new FpcPositionMessage(referenceHub), 0);
				}
			}
		}

		private static void EnsureArrayCapacity()
		{
			int num = Mathf.Min(FpcServerPositionDistributor._bufferPlayerIDs.Length, FpcServerPositionDistributor._bufferSyncData.Length);
			int count = ReferenceHub.AllHubs.Count;
			if (count <= num - 5)
			{
				return;
			}
			FpcServerPositionDistributor._bufferPlayerIDs = new int[count + 10];
			FpcServerPositionDistributor._bufferSyncData = new FpcSyncData[count + 10];
		}

		public static void WriteAll(ReferenceHub receiver, NetworkWriter writer)
		{
			ushort num = 0;
			ICustomVisibilityRole customVisibilityRole = receiver.roleManager.CurrentRole as ICustomVisibilityRole;
			bool flag;
			VisibilityController visibilityController;
			if (customVisibilityRole != null)
			{
				flag = true;
				visibilityController = customVisibilityRole.VisibilityController;
			}
			else
			{
				flag = false;
				visibilityController = null;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (referenceHub.netId != receiver.netId)
				{
					IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
					if (fpcRole != null)
					{
						bool flag2 = flag && !visibilityController.ValidateVisibility(referenceHub);
						FpcSyncData newSyncData = FpcServerPositionDistributor.GetNewSyncData(receiver, referenceHub, fpcRole.FpcModule, flag2);
						if (!flag2)
						{
							FpcServerPositionDistributor._bufferPlayerIDs[(int)num] = referenceHub.PlayerId;
							FpcServerPositionDistributor._bufferSyncData[(int)num] = newSyncData;
							num += 1;
						}
					}
				}
			}
			writer.WriteUShort(num);
			for (int i = 0; i < (int)num; i++)
			{
				writer.WriteRecyclablePlayerId(new RecyclablePlayerId(FpcServerPositionDistributor._bufferPlayerIDs[i]));
				FpcServerPositionDistributor._bufferSyncData[i].Write(writer);
			}
		}

		private static FpcSyncData GetNewSyncData(ReferenceHub receiver, ReferenceHub target, FirstPersonMovementModule fpmm, bool isInvisible)
		{
			FpcSyncData prevSyncData = FpcServerPositionDistributor.GetPrevSyncData(receiver, target);
			FpcSyncData fpcSyncData = (isInvisible ? default(FpcSyncData) : new FpcSyncData(prevSyncData, fpmm.SyncMovementState, fpmm.IsGrounded, new RelativePosition(target.transform.position), fpmm.MouseLook));
			FpcServerPositionDistributor.PreviouslySent[receiver.netId][target.netId] = fpcSyncData;
			return fpcSyncData;
		}

		private static FpcSyncData GetPrevSyncData(ReferenceHub receiver, ReferenceHub target)
		{
			Dictionary<uint, FpcSyncData> dictionary;
			if (!FpcServerPositionDistributor.PreviouslySent.TryGetValue(receiver.netId, out dictionary))
			{
				FpcServerPositionDistributor.PreviouslySent.Add(receiver.netId, new Dictionary<uint, FpcSyncData>());
				return default(FpcSyncData);
			}
			FpcSyncData fpcSyncData;
			if (!dictionary.TryGetValue(target.netId, out fpcSyncData))
			{
				return default(FpcSyncData);
			}
			return fpcSyncData;
		}

		private const int MinTickrate = 10;

		private const int MaxTickrate = 60;

		private const int ArrayStartSize = 30;

		private const int ArrayAddAmount = 10;

		private const int ArrayAddThreshold = 5;

		private static readonly Dictionary<uint, Dictionary<uint, FpcSyncData>> PreviouslySent = new Dictionary<uint, Dictionary<uint, FpcSyncData>>();

		private static int[] _bufferPlayerIDs = new int[30];

		private static FpcSyncData[] _bufferSyncData = new FpcSyncData[30];

		private static float _sendCooldown;
	}
}
