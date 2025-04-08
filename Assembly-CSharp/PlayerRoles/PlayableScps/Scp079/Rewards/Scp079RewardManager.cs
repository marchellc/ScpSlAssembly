using System;
using System.Collections.Generic;
using GameObjectPools;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp079.Rewards
{
	public class Scp079RewardManager : SubroutineBase, IPoolResettable
	{
		private static double CurTime
		{
			get
			{
				return NetworkTime.time;
			}
		}

		public void MarkRoom(RoomIdentifier room)
		{
			this._markedRooms[room] = Scp079RewardManager.CurTime;
		}

		public void MarkRooms(RoomIdentifier[] rooms)
		{
			foreach (RoomIdentifier roomIdentifier in rooms)
			{
				this.MarkRoom(roomIdentifier);
			}
		}

		public void ResetObject()
		{
			this._markedRooms.Clear();
		}

		public static bool CheckForRoomInteractions(ReferenceHub scp079Player, RoomIdentifier room)
		{
			Scp079Role scp079Role = scp079Player.roleManager.CurrentRole as Scp079Role;
			return scp079Role != null && Scp079RewardManager.CheckForRoomInteractions(scp079Role, room);
		}

		public static bool CheckForRoomInteractions(RoomIdentifier room)
		{
			using (HashSet<Scp079Role>.Enumerator enumerator = Scp079Role.ActiveInstances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (Scp079RewardManager.CheckForRoomInteractions(enumerator.Current, room))
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool CheckForRoomInteractions(Scp079Role scp079, RoomIdentifier room)
		{
			Scp079RewardManager scp079RewardManager;
			double num;
			return scp079.SubroutineModule.TryGetSubroutine<Scp079RewardManager>(out scp079RewardManager) && scp079RewardManager._markedRooms.TryGetValue(room, out num) && Scp079RewardManager.CurTime - num <= 12.0;
		}

		public static void GrantExp(Scp079Role instance, int reward, Scp079HudTranslation gainReason, RoleTypeId subject = RoleTypeId.None)
		{
			ReferenceHub referenceHub;
			if (!instance.TryGetOwner(out referenceHub))
			{
				return;
			}
			Scp079GainingExperienceEventArgs scp079GainingExperienceEventArgs = new Scp079GainingExperienceEventArgs(referenceHub, (float)reward, gainReason);
			Scp079Events.OnGainingExperience(scp079GainingExperienceEventArgs);
			if (!scp079GainingExperienceEventArgs.IsAllowed)
			{
				return;
			}
			reward = (int)scp079GainingExperienceEventArgs.Amount;
			gainReason = scp079GainingExperienceEventArgs.Reason;
			Scp079TierManager scp079TierManager;
			if (!instance.SubroutineModule.TryGetSubroutine<Scp079TierManager>(out scp079TierManager))
			{
				return;
			}
			scp079TierManager.ServerGrantExperience(reward, gainReason, subject);
			Scp079Events.OnGainedExperience(new Scp079GainedExperienceEventArgs(referenceHub, (float)reward, gainReason));
		}

		private const float MarkDuration = 12f;

		private readonly Dictionary<RoomIdentifier, double> _markedRooms = new Dictionary<RoomIdentifier, double>();
	}
}
