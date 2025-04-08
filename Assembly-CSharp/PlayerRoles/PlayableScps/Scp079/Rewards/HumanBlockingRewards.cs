using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Rewards
{
	public static class HumanBlockingRewards
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Scp079DoorAbility.OnServerAnyDoorInteraction += HumanBlockingRewards.ProcessBlockage;
		}

		private static void ProcessBlockage(Scp079Role role, DoorVariant dv)
		{
			if (dv.TargetState)
			{
				return;
			}
			if (HumanBlockingRewards._lastBlockTime + 5.0 > NetworkTime.time)
			{
				return;
			}
			Vector3 position = dv.transform.position;
			RoomIdentifier[] rooms = dv.Rooms;
			for (int i = 0; i < rooms.Length; i++)
			{
				if (HumanBlockingRewards.CheckRoom(rooms[i], position))
				{
					Scp079RewardManager.GrantExp(role, 5, Scp079HudTranslation.ExpGainBlockingHuman, RoleTypeId.None);
					HumanBlockingRewards._lastBlockTime = NetworkTime.time;
					return;
				}
			}
		}

		private static bool CheckRoom(RoomIdentifier room, Vector3 doorPos)
		{
			HumanBlockingRewards.RoomScps.Clear();
			HumanBlockingRewards.RoomHumans.Clear();
			bool flag = false;
			bool flag2 = false;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					FirstPersonMovementModule fpcModule = fpcRole.FpcModule;
					if (!(RoomUtils.RoomAtPosition(fpcModule.Position) != room))
					{
						if (referenceHub.IsSCP(true))
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
			}
			if (!flag2 || !flag)
			{
				return false;
			}
			foreach (FirstPersonMovementModule firstPersonMovementModule in HumanBlockingRewards.RoomScps)
			{
				Vector3 vector = HumanBlockingRewards.NormalizeIgnoreY(firstPersonMovementModule.Motor.MoveDirection.normalized);
				Vector3 vector2 = HumanBlockingRewards.NormalizeIgnoreY(doorPos - firstPersonMovementModule.Position);
				if (Vector3.Dot(vector, vector2) >= 0.5f)
				{
					foreach (FirstPersonMovementModule firstPersonMovementModule2 in HumanBlockingRewards.RoomHumans)
					{
						Vector3 vector3 = firstPersonMovementModule2.Position - firstPersonMovementModule.Position;
						if (vector3.sqrMagnitude <= 400f && Vector3.Dot(vector, HumanBlockingRewards.NormalizeIgnoreY(vector3)) >= 0.5f)
						{
							return true;
						}
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

		private const float MinDot = 0.5f;

		private const float Cooldown = 5f;

		private const int Reward = 5;

		private const int SqrDistanceCutoff = 400;

		private static double _lastBlockTime;

		private static readonly HashSet<FirstPersonMovementModule> RoomScps = new HashSet<FirstPersonMovementModule>();

		private static readonly HashSet<FirstPersonMovementModule> RoomHumans = new HashSet<FirstPersonMovementModule>();
	}
}
