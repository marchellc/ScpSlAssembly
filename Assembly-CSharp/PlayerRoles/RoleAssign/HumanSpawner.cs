using System;
using System.Collections.Generic;
using Utils.NonAllocLINQ;

namespace PlayerRoles.RoleAssign;

public static class HumanSpawner
{
	private class RoleHistory
	{
		private int _clock;

		private RoleTypeId[] _history;

		public RoleTypeId[] History
		{
			get
			{
				if (this._history == null)
				{
					this._history = new RoleTypeId[5];
					for (int i = 0; i < 5; i++)
					{
						this._history[i] = RoleTypeId.None;
					}
				}
				return this._history;
			}
		}

		public void RegisterRole(RoleTypeId role)
		{
			this.History[this._clock++ % 5] = role;
		}
	}

	private const RoleTypeId DefaultHumanRole = RoleTypeId.ClassD;

	private const int HistorySize = 5;

	private static Team[] _humanQueue;

	private static int _queueClock;

	private static int _queueLength;

	private static readonly Dictionary<Team, IHumanSpawnHandler> Handlers = new Dictionary<Team, IHumanSpawnHandler>
	{
		[Team.ClassD] = new OneRoleHumanSpawner(RoleTypeId.ClassD),
		[Team.FoundationForces] = new OneRoleHumanSpawner(RoleTypeId.FacilityGuard),
		[Team.Scientists] = new OneRoleHumanSpawner(RoleTypeId.Scientist)
	};

	private static readonly Dictionary<string, RoleHistory> History = new Dictionary<string, RoleHistory>();

	private static readonly List<ReferenceHub> Candidates = new List<ReferenceHub>();

	private static RoleTypeId NextHumanRoleToSpawn
	{
		get
		{
			if (HumanSpawner._queueLength == 0)
			{
				throw new InvalidOperationException("Failed to get next role to spawn, queue has no human roles.");
			}
			Team key = HumanSpawner._humanQueue[HumanSpawner._queueClock++ % HumanSpawner._queueLength];
			if (!HumanSpawner.Handlers.TryGetValue(key, out var value))
			{
				return RoleTypeId.ClassD;
			}
			return value.NextRole;
		}
	}

	public static void SpawnHumans(Team[] queue, int queueLength)
	{
		HumanSpawner._humanQueue = queue;
		HumanSpawner._queueClock = 0;
		HumanSpawner._queueLength = queueLength;
		int num = ReferenceHub.AllHubs.Count(RoleAssigner.CheckPlayer);
		RoleTypeId[] array = new RoleTypeId[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = HumanSpawner.NextHumanRoleToSpawn;
		}
		array.ShuffleList();
		for (int j = 0; j < num; j++)
		{
			HumanSpawner.AssignHumanRoleToRandomPlayer(array[j]);
		}
	}

	public static void SpawnLate(ReferenceHub ply)
	{
		ply.roleManager.ServerSetRole(HumanSpawner.NextHumanRoleToSpawn, RoleChangeReason.LateJoin);
	}

	private static void AssignHumanRoleToRandomPlayer(RoleTypeId role)
	{
		HumanSpawner.Candidates.Clear();
		int num = int.MaxValue;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (!RoleAssigner.CheckPlayer(allHub))
			{
				continue;
			}
			RoleHistory orAdd = HumanSpawner.History.GetOrAdd(allHub.authManager.UserId, () => new RoleHistory());
			int num2 = 0;
			for (int num3 = 0; num3 < 5; num3++)
			{
				if (orAdd.History[num3] == role)
				{
					num2++;
				}
			}
			if (num2 <= num)
			{
				if (num2 < num)
				{
					HumanSpawner.Candidates.Clear();
				}
				HumanSpawner.Candidates.Add(allHub);
				num = num2;
			}
		}
		if (HumanSpawner.Candidates.Count != 0)
		{
			ReferenceHub referenceHub = HumanSpawner.Candidates.RandomItem();
			referenceHub.roleManager.ServerSetRole(role, RoleChangeReason.RoundStart);
			HumanSpawner.History[referenceHub.authManager.UserId].RegisterRole(role);
		}
	}
}
