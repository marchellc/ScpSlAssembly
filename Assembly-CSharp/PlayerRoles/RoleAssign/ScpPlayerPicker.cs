using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

namespace PlayerRoles.RoleAssign
{
	public static class ScpPlayerPicker
	{
		public static List<ReferenceHub> ChoosePlayers(int targetScps)
		{
			using (ScpTicketsLoader scpTicketsLoader = new ScpTicketsLoader())
			{
				ScpPlayerPicker.GenerateList(scpTicketsLoader, targetScps);
				foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
				{
					if (RoleAssigner.CheckPlayer(referenceHub))
					{
						int tickets = scpTicketsLoader.GetTickets(referenceHub, 10);
						scpTicketsLoader.ModifyTickets(referenceHub, tickets + 2);
					}
				}
				foreach (ReferenceHub referenceHub2 in ScpPlayerPicker.ScpsToSpawn)
				{
					scpTicketsLoader.ModifyTickets(referenceHub2, 10);
				}
			}
			if (targetScps != ScpPlayerPicker.ScpsToSpawn.Count)
			{
				throw new InvalidOperationException("Failed to meet target number of SCPs.");
			}
			return ScpPlayerPicker.ScpsToSpawn;
		}

		private static void GenerateList(ScpTicketsLoader loader, int scpsToAssign)
		{
			ScpPlayerPicker.ScpsToSpawn.Clear();
			if (scpsToAssign <= 0)
			{
				return;
			}
			int num = 0;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (RoleAssigner.CheckPlayer(referenceHub))
				{
					int tickets = loader.GetTickets(referenceHub, 10);
					if (tickets >= num)
					{
						if (tickets > num)
						{
							ScpPlayerPicker.ScpsToSpawn.Clear();
						}
						num = tickets;
						ScpPlayerPicker.ScpsToSpawn.Add(referenceHub);
					}
				}
			}
			if (ScpPlayerPicker.ScpsToSpawn.Count > 1)
			{
				ReferenceHub referenceHub2 = ScpPlayerPicker.ScpsToSpawn.RandomItem<ReferenceHub>();
				ScpPlayerPicker.ScpsToSpawn.Clear();
				ScpPlayerPicker.ScpsToSpawn.Add(referenceHub2);
			}
			scpsToAssign -= ScpPlayerPicker.ScpsToSpawn.Count;
			if (scpsToAssign <= 0)
			{
				return;
			}
			List<ScpPlayerPicker.PotentialScp> list = ListPool<ScpPlayerPicker.PotentialScp>.Shared.Rent();
			long num2 = 0L;
			using (HashSet<ReferenceHub>.Enumerator enumerator = ReferenceHub.AllHubs.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ReferenceHub referenceHub3 = enumerator.Current;
					if (!ScpPlayerPicker.ScpsToSpawn.Contains(referenceHub3) && RoleAssigner.CheckPlayer(referenceHub3))
					{
						long num3 = 1L;
						int tickets2 = loader.GetTickets(referenceHub3, 10);
						for (int i = 0; i < scpsToAssign; i++)
						{
							num3 *= (long)tickets2;
						}
						list.Add(new ScpPlayerPicker.PotentialScp
						{
							Player = referenceHub3,
							Weight = num3
						});
						num2 += num3;
					}
				}
				goto IL_01C4;
			}
			IL_0156:
			double num4 = (double)global::UnityEngine.Random.value * (double)num2;
			for (int j = 0; j < list.Count; j++)
			{
				ScpPlayerPicker.PotentialScp potentialScp = list[j];
				num4 -= (double)potentialScp.Weight;
				if (num4 <= 0.0)
				{
					scpsToAssign--;
					ScpPlayerPicker.ScpsToSpawn.Add(potentialScp.Player);
					list.RemoveAt(j);
					num2 -= potentialScp.Weight;
					break;
				}
			}
			IL_01C4:
			if (scpsToAssign <= 0)
			{
				ListPool<ScpPlayerPicker.PotentialScp>.Shared.Return(list);
				return;
			}
			goto IL_0156;
		}

		private const int DefaultTickets = 10;

		private const int HumanTicketBonus = 2;

		private static readonly List<ReferenceHub> ScpsToSpawn = new List<ReferenceHub>();

		private class PotentialScp
		{
			public ReferenceHub Player;

			public long Weight;
		}
	}
}
