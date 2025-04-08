using System;
using System.Collections.Generic;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace PlayerRoles.RoleAssign
{
	public static class ScpSpawner
	{
		private static PlayerRoleBase[] SpawnableScps
		{
			get
			{
				if (ScpSpawner._cacheSet)
				{
					return ScpSpawner._cachedSpawnableScps;
				}
				List<PlayerRoleBase> list = new List<PlayerRoleBase>();
				foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
				{
					if (keyValuePair.Value is ISpawnableScp)
					{
						list.Add(keyValuePair.Value);
					}
				}
				ScpSpawner._cacheSet = true;
				ScpSpawner._chancesArray = new float[list.Count];
				return ScpSpawner._cachedSpawnableScps = list.ToArray();
			}
		}

		private static RoleTypeId NextScp
		{
			get
			{
				float num = 0f;
				int num2 = ScpSpawner.SpawnableScps.Length;
				for (int i = 0; i < num2; i++)
				{
					PlayerRoleBase playerRoleBase = ScpSpawner.SpawnableScps[i];
					if (ScpSpawner.EnqueuedScps.Contains(playerRoleBase.RoleTypeId))
					{
						ScpSpawner._chancesArray[i] = 0f;
					}
					else
					{
						float num3 = (ScpSpawner.SpawnableScps[i] as ISpawnableScp).GetSpawnChance(ScpSpawner.EnqueuedScps);
						num3 = Mathf.Max(num3, 0f);
						num += num3;
						ScpSpawner._chancesArray[i] = num3;
					}
				}
				if (num == 0f)
				{
					return ScpSpawner.RandomLeastFrequentScp;
				}
				float num4 = global::UnityEngine.Random.Range(0f, num);
				for (int j = 0; j < num2; j++)
				{
					num4 -= ScpSpawner._chancesArray[j];
					if (num4 < 0f)
					{
						return ScpSpawner.SpawnableScps[j].RoleTypeId;
					}
				}
				return ScpSpawner.SpawnableScps[num2 - 1].RoleTypeId;
			}
		}

		private static RoleTypeId RandomLeastFrequentScp
		{
			get
			{
				int num = ScpSpawner.SpawnableScps.Length;
				int num2 = int.MaxValue;
				for (int i = 0; i < num; i++)
				{
					RoleTypeId roleTypeId = ScpSpawner.SpawnableScps[i].RoleTypeId;
					int num3 = 0;
					using (List<RoleTypeId>.Enumerator enumerator = ScpSpawner.EnqueuedScps.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current == roleTypeId)
							{
								num3++;
							}
						}
					}
					if (num3 <= num2)
					{
						if (num3 < num2)
						{
							ScpSpawner.BackupScps.Clear();
						}
						ScpSpawner.BackupScps.Add(roleTypeId);
						num2 = num3;
					}
				}
				return ScpSpawner.BackupScps.RandomItem<RoleTypeId>();
			}
		}

		public static void SpawnScps(int targetScpNumber)
		{
			ScpSpawner.EnqueuedScps.Clear();
			for (int i = 0; i < targetScpNumber; i++)
			{
				ScpSpawner.EnqueuedScps.Add(ScpSpawner.NextScp);
			}
			List<ReferenceHub> list = ScpPlayerPicker.ChoosePlayers(targetScpNumber);
			while (ScpSpawner.EnqueuedScps.Count > 0)
			{
				RoleTypeId roleTypeId = ScpSpawner.EnqueuedScps[0];
				ScpSpawner.EnqueuedScps.RemoveAt(0);
				ScpSpawner.AssignScp(list, roleTypeId, ScpSpawner.EnqueuedScps);
			}
		}

		private static void AssignScp(List<ReferenceHub> chosenPlayers, RoleTypeId scp, List<RoleTypeId> otherScps)
		{
			ScpSpawner.ChancesBuffer.Clear();
			int num = 1;
			int num2 = 0;
			foreach (ReferenceHub referenceHub in chosenPlayers)
			{
				int num3 = ScpSpawner.GetPreferenceOfPlayer(referenceHub, scp);
				foreach (RoleTypeId roleTypeId in otherScps)
				{
					num3 -= ScpSpawner.GetPreferenceOfPlayer(referenceHub, roleTypeId);
				}
				num2++;
				ScpSpawner.ChancesBuffer[referenceHub] = (float)num3;
				num = Mathf.Min(num3, num);
			}
			float num4 = 0f;
			ScpSpawner.SelectedSpawnChances.Clear();
			foreach (KeyValuePair<ReferenceHub, float> keyValuePair in ScpSpawner.ChancesBuffer)
			{
				float num5 = Mathf.Pow(keyValuePair.Value - (float)num + 1f, (float)num2);
				ScpSpawner.SelectedSpawnChances[keyValuePair.Key] = num5;
				num4 += num5;
			}
			float num6 = num4 * global::UnityEngine.Random.value;
			float num7 = 0f;
			foreach (KeyValuePair<ReferenceHub, float> keyValuePair2 in ScpSpawner.SelectedSpawnChances)
			{
				num7 += keyValuePair2.Value;
				if (num7 >= num6)
				{
					ReferenceHub key = keyValuePair2.Key;
					chosenPlayers.Remove(key);
					key.roleManager.ServerSetRole(scp, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
					break;
				}
			}
		}

		private static int GetPreferenceOfPlayer(ReferenceHub ply, RoleTypeId scp)
		{
			int connectionId = ply.connectionToClient.connectionId;
			ScpSpawnPreferences.SpawnPreferences spawnPreferences;
			if (!ScpSpawnPreferences.Preferences.TryGetValue(connectionId, out spawnPreferences))
			{
				return 0;
			}
			int num;
			if (!spawnPreferences.Preferences.TryGetValue(scp, out num))
			{
				num = 0;
			}
			return num;
		}

		private static readonly Dictionary<ReferenceHub, float> SelectedSpawnChances = new Dictionary<ReferenceHub, float>();

		private static readonly Dictionary<ReferenceHub, float> ChancesBuffer = new Dictionary<ReferenceHub, float>();

		private static readonly List<RoleTypeId> BackupScps = new List<RoleTypeId>(8);

		private static readonly List<RoleTypeId> EnqueuedScps = new List<RoleTypeId>(8);

		private static PlayerRoleBase[] _cachedSpawnableScps;

		private static float[] _chancesArray;

		private static bool _cacheSet;
	}
}
