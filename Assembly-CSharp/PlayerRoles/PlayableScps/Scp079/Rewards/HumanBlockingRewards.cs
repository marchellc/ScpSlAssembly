using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Rewards;

public static class HumanBlockingRewards
{
	private const float MinDot = 0.5f;

	private const float Cooldown = 5f;

	private const int Reward = 5;

	private const int SqrDistanceCutoff = 400;

	private static double _lastBlockTime;

	private static readonly HashSet<FirstPersonMovementModule> RoomScps = new HashSet<FirstPersonMovementModule>();

	private static readonly HashSet<FirstPersonMovementModule> RoomHumans = new HashSet<FirstPersonMovementModule>();

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Scp079DoorAbility.OnServerAnyDoorInteraction += ProcessBlockage;
	}

	private static void ProcessBlockage(Scp079Role role, DoorVariant dv)
	{
		if (dv.TargetState || HumanBlockingRewards._lastBlockTime + 5.0 > NetworkTime.time)
		{
			return;
		}
		Vector3 position = dv.transform.position;
		RoomIdentifier[] rooms = dv.Rooms;
		for (int i = 0; i < rooms.Length; i++)
		{
			if (HumanBlockingRewards.CheckRoom(rooms[i], position))
			{
				Scp079RewardManager.GrantExp(role, 5, Scp079HudTranslation.ExpGainBlockingHuman);
				HumanBlockingRewards._lastBlockTime = NetworkTime.time;
				break;
			}
		}
	}

	private static bool CheckRoom(RoomIdentifier room, Vector3 doorPos)
	{
		HumanBlockingRewards.RoomScps.Clear();
		HumanBlockingRewards.RoomHumans.Clear();
		bool flag = false;
		bool flag2 = false;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is IFpcRole { FpcModule: var fpcModule } && allHub.TryGetCurrentRoom(out var room2) && !(room2 != room))
			{
				if (allHub.IsSCP())
				{
					HumanBlockingRewards.RoomScps.Add(fpcModule);
					flag = true;
				}
				else
				{
					HumanBlockingRewards.RoomHumans.Add(fpcModule);
					flag2 = true;
				}
			}
		}
		if (!flag2 || !flag)
		{
			return false;
		}
		foreach (FirstPersonMovementModule roomScp in HumanBlockingRewards.RoomScps)
		{
			Vector3 lhs = HumanBlockingRewards.NormalizeIgnoreY(roomScp.Motor.MoveDirection.normalized);
			Vector3 rhs = HumanBlockingRewards.NormalizeIgnoreY(doorPos - roomScp.Position);
			if (Vector3.Dot(lhs, rhs) < 0.5f)
			{
				continue;
			}
			foreach (FirstPersonMovementModule roomHuman in HumanBlockingRewards.RoomHumans)
			{
				Vector3 direction = roomHuman.Position - roomScp.Position;
				if (!(direction.sqrMagnitude > 400f) && Vector3.Dot(lhs, HumanBlockingRewards.NormalizeIgnoreY(direction)) >= 0.5f)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static Vector3 NormalizeIgnoreY(Vector3 direction)
	{
		direction.y = 0f;
		return direction.normalized;
	}
}
