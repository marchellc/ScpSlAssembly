using System;
using System.Collections.Generic;
using Utils.NonAllocLINQ;

namespace PlayerRoles.RoleAssign
{
	public static class HumanSpawner
	{
		private static RoleTypeId NextHumanRoleToSpawn
		{
			get
			{
				if (HumanSpawner._queueLength == 0)
				{
					throw new InvalidOperationException("Failed to get next role to spawn, queue has no human roles.");
				}
				Team team = HumanSpawner._humanQueue[HumanSpawner._queueClock++ % HumanSpawner._queueLength];
				IHumanSpawnHandler humanSpawnHandler;
				if (!HumanSpawner.Handlers.TryGetValue(team, out humanSpawnHandler))
				{
					return RoleTypeId.ClassD;
				}
				return humanSpawnHandler.NextRole;
			}
		}

		public static void SpawnHumans(Team[] queue, int queueLength)
		{
			HumanSpawner._humanQueue = queue;
			HumanSpawner._queueClock = 0;
			HumanSpawner._queueLength = queueLength;
			int num = ReferenceHub.AllHubs.Count(new Func<ReferenceHub, bool>(RoleAssigner.CheckPlayer));
			RoleTypeId[] array = new RoleTypeId[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = HumanSpawner.NextHumanRoleToSpawn;
			}
			array.ShuffleList(null);
			for (int j = 0; j < num; j++)
			{
				HumanSpawner.AssignHumanRoleToRandomPlayer(array[j]);
			}
		}

		public static void SpawnLate(ReferenceHub ply)
		{
			ply.roleManager.ServerSetRole(HumanSpawner.NextHumanRoleToSpawn, RoleChangeReason.LateJoin, RoleSpawnFlags.All);
		}

		private static void AssignHumanRoleToRandomPlayer(RoleTypeId role)
		{
			HumanSpawner.Candidates.Clear();
			int num = int.MaxValue;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (RoleAssigner.CheckPlayer(referenceHub))
				{
					HumanSpawner.RoleHistory orAdd = HumanSpawner.History.GetOrAdd(referenceHub.authManager.UserId, () => new HumanSpawner.RoleHistory());
					int num2 = 0;
					for (int i = 0; i < 5; i++)
					{
						if (orAdd.History[i] == role)
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
						HumanSpawner.Candidates.Add(referenceHub);
						num = num2;
					}
				}
			}
			if (HumanSpawner.Candidates.Count == 0)
			{
				return;
			}
			ReferenceHub referenceHub2 = HumanSpawner.Candidates.RandomItem<ReferenceHub>();
			referenceHub2.roleManager.ServerSetRole(role, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
			HumanSpawner.History[referenceHub2.authManager.UserId].RegisterRole(role);
		}

		// Note: this type is marked as 'beforefieldinit'.
		static HumanSpawner()
		{
			Dictionary<Team, IHumanSpawnHandler> dictionary = new Dictionary<Team, IHumanSpawnHandler>();
			dictionary[Team.ClassD] = new OneRoleHumanSpawner(RoleTypeId.ClassD);
			dictionary[Team.FoundationForces] = new OneRoleHumanSpawner(RoleTypeId.FacilityGuard);
			dictionary[Team.Scientists] = new OneRoleHumanSpawner(RoleTypeId.Scientist);
			HumanSpawner.Handlers = dictionary;
			HumanSpawner.History = new Dictionary<string, HumanSpawner.RoleHistory>();
			HumanSpawner.Candidates = new List<ReferenceHub>();
		}

		private const RoleTypeId DefaultHumanRole = RoleTypeId.ClassD;

		private const int HistorySize = 5;

		private static Team[] _humanQueue;

		private static int _queueClock;

		private static int _queueLength;

		private static readonly Dictionary<Team, IHumanSpawnHandler> Handlers;

		private static readonly Dictionary<string, HumanSpawner.RoleHistory> History;

		private static readonly List<ReferenceHub> Candidates;

		private class RoleHistory
		{
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
				RoleTypeId[] history = this.History;
				int clock = this._clock;
				this._clock = clock + 1;
				history[clock % 5] = role;
			}

			private int _clock;

			private RoleTypeId[] _history;
		}
	}
}
