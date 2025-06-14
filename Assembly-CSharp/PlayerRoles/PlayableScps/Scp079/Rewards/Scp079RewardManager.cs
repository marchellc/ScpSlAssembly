using System.Collections.Generic;
using GameObjectPools;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Rewards;

public class Scp079RewardManager : SubroutineBase, IPoolResettable
{
	private const float MarkDuration = 12f;

	private readonly Dictionary<RoomIdentifier, double> _markedRooms = new Dictionary<RoomIdentifier, double>();

	private static double CurTime => NetworkTime.time;

	public void MarkRoom(RoomIdentifier room)
	{
		this._markedRooms[room] = Scp079RewardManager.CurTime;
	}

	public void MarkRooms(RoomIdentifier[] rooms)
	{
		foreach (RoomIdentifier room in rooms)
		{
			this.MarkRoom(room);
		}
	}

	public void ResetObject()
	{
		this._markedRooms.Clear();
	}

	public static bool CheckForRoomInteractions(ReferenceHub scp079Player, RoomIdentifier room)
	{
		if (scp079Player.roleManager.CurrentRole is Scp079Role scp)
		{
			return Scp079RewardManager.CheckForRoomInteractions(scp, room);
		}
		return false;
	}

	public static bool CheckForRoomInteractions(RoomIdentifier room)
	{
		foreach (Scp079Role activeInstance in Scp079Role.ActiveInstances)
		{
			if (Scp079RewardManager.CheckForRoomInteractions(activeInstance, room))
			{
				return true;
			}
		}
		return false;
	}

	public static bool CheckForRoomInteractions(Vector3 roomPosition)
	{
		if (roomPosition.TryGetRoom(out var room))
		{
			return Scp079RewardManager.CheckForRoomInteractions(room);
		}
		return false;
	}

	public static bool CheckForRoomInteractions(Scp079Role scp079, RoomIdentifier room)
	{
		if (!scp079.SubroutineModule.TryGetSubroutine<Scp079RewardManager>(out var subroutine))
		{
			return false;
		}
		if (!subroutine._markedRooms.TryGetValue(room, out var value))
		{
			return false;
		}
		if (Scp079RewardManager.CurTime - value > 12.0)
		{
			return false;
		}
		return true;
	}

	public static void GrantExp(Scp079Role instance, int reward, Scp079HudTranslation gainReason, RoleTypeId subject = RoleTypeId.None)
	{
		if (!instance.TryGetOwner(out var hub))
		{
			return;
		}
		Scp079GainingExperienceEventArgs e = new Scp079GainingExperienceEventArgs(hub, reward, gainReason);
		Scp079Events.OnGainingExperience(e);
		if (e.IsAllowed)
		{
			reward = (int)e.Amount;
			gainReason = e.Reason;
			if (instance.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out var subroutine))
			{
				subroutine.ServerGrantExperience(reward, gainReason, subject);
				Scp079Events.OnGainedExperience(new Scp079GainedExperienceEventArgs(hub, reward, gainReason));
			}
		}
	}
}
