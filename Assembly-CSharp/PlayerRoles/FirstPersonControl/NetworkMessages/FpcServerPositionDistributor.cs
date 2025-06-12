using System.Collections.Generic;
using CentralAuth;
using InventorySystem;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.Visibility;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.NetworkMessages;

public static class FpcServerPositionDistributor
{
	private const int MinTickrate = 10;

	private const int MaxTickrate = 60;

	private const int ArrayStartSize = 30;

	private const int ArrayAddAmount = 10;

	private const int ArrayAddThreshold = 5;

	private static readonly Dictionary<uint, Dictionary<uint, FpcSyncData>> PreviouslySent = new Dictionary<uint, Dictionary<uint, FpcSyncData>>();

	private static int[] _bufferPlayerIDs = new int[30];

	private static FpcSyncData[] _bufferSyncData = new FpcSyncData[30];

	private static float _sendCooldown;

	private static float SendRate => 1f / (float)Mathf.Clamp(ServerStatic.ServerTickrate, 10, 60);

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnLateUpdate += LateUpdate;
		PlayerRoleManager.OnRoleChanged += ResetPlayer;
		Inventory.OnServerStarted += FpcServerPositionDistributor.PreviouslySent.Clear;
		ReferenceHub.OnPlayerAdded += delegate
		{
			FpcServerPositionDistributor.EnsureArrayCapacity();
		};
	}

	private static void ResetPlayer(ReferenceHub userHub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (!(prevRole is IFpcRole))
		{
			return;
		}
		uint netId = userHub.netId;
		foreach (KeyValuePair<uint, Dictionary<uint, FpcSyncData>> item in FpcServerPositionDistributor.PreviouslySent)
		{
			item.Value.Remove(netId);
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
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.Mode != ClientInstanceMode.Unverified && !allHub.isLocalPlayer)
			{
				allHub.connectionToClient.Send(new FpcPositionMessage(allHub));
			}
		}
	}

	private static void EnsureArrayCapacity()
	{
		int num = Mathf.Min(FpcServerPositionDistributor._bufferPlayerIDs.Length, FpcServerPositionDistributor._bufferSyncData.Length);
		int count = ReferenceHub.AllHubs.Count;
		if (count > num - 5)
		{
			FpcServerPositionDistributor._bufferPlayerIDs = new int[count + 10];
			FpcServerPositionDistributor._bufferSyncData = new FpcSyncData[count + 10];
		}
	}

	public static void WriteAll(ReferenceHub receiver, NetworkWriter writer)
	{
		ushort num = 0;
		bool flag;
		VisibilityController visibilityController;
		if (receiver.roleManager.CurrentRole is ICustomVisibilityRole customVisibilityRole)
		{
			flag = true;
			visibilityController = customVisibilityRole.VisibilityController;
		}
		else
		{
			flag = false;
			visibilityController = null;
		}
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.netId != receiver.netId && allHub.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				bool flag2 = flag && !visibilityController.ValidateVisibility(allHub);
				PlayerValidatedVisibilityEventArgs e = new PlayerValidatedVisibilityEventArgs(receiver, allHub, !flag2);
				PlayerEvents.OnValidatedVisibility(e);
				flag2 = !e.IsVisible;
				FpcSyncData newSyncData = FpcServerPositionDistributor.GetNewSyncData(receiver, allHub, fpcRole.FpcModule, flag2);
				if (!flag2)
				{
					FpcServerPositionDistributor._bufferPlayerIDs[num] = allHub.PlayerId;
					FpcServerPositionDistributor._bufferSyncData[num] = newSyncData;
					num++;
				}
			}
		}
		writer.WriteUShort(num);
		for (int i = 0; i < num; i++)
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
		if (!FpcServerPositionDistributor.PreviouslySent.TryGetValue(receiver.netId, out var value))
		{
			FpcServerPositionDistributor.PreviouslySent.Add(receiver.netId, new Dictionary<uint, FpcSyncData>());
			return default(FpcSyncData);
		}
		if (!value.TryGetValue(target.netId, out var value2))
		{
			return default(FpcSyncData);
		}
		return value2;
	}
}
