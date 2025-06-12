using System.Collections.Generic;
using PlayerRoles.PlayableScps;
using UnityEngine;

namespace PlayerRoles.RoleAssign;

public static class ScpSpawner
{
	private static readonly Dictionary<ReferenceHub, float> SelectedSpawnChances = new Dictionary<ReferenceHub, float>();

	private static readonly Dictionary<ReferenceHub, float> ChancesBuffer = new Dictionary<ReferenceHub, float>();

	private static readonly List<RoleTypeId> BackupScps = new List<RoleTypeId>(8);

	private static readonly List<RoleTypeId> EnqueuedScps = new List<RoleTypeId>(8);

	private static PlayerRoleBase[] _cachedSpawnableScps;

	private static float[] _chancesArray;

	private static bool _cacheSet;

	public static int MaxSpawnableScps => ScpSpawner.SpawnableScps.Length;

	private static PlayerRoleBase[] SpawnableScps
	{
		get
		{
			if (ScpSpawner._cacheSet)
			{
				return ScpSpawner._cachedSpawnableScps;
			}
			List<PlayerRoleBase> list = new List<PlayerRoleBase>();
			foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> allRole in PlayerRoleLoader.AllRoles)
			{
				if (allRole.Value is ISpawnableScp)
				{
					list.Add(allRole.Value);
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
					continue;
				}
				float spawnChance = (ScpSpawner.SpawnableScps[i] as ISpawnableScp).GetSpawnChance(ScpSpawner.EnqueuedScps);
				spawnChance = Mathf.Max(spawnChance, 0f);
				num += spawnChance;
				ScpSpawner._chancesArray[i] = spawnChance;
			}
			if (num == 0f)
			{
				return ScpSpawner.RandomLeastFrequentScp;
			}
			float num3 = Random.Range(0f, num);
			for (int j = 0; j < num2; j++)
			{
				num3 -= ScpSpawner._chancesArray[j];
				if (!(num3 >= 0f))
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
				foreach (RoleTypeId enqueuedScp in ScpSpawner.EnqueuedScps)
				{
					if (enqueuedScp == roleTypeId)
					{
						num3++;
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
			return ScpSpawner.BackupScps.RandomItem();
		}
	}

	public static void SpawnScps(int targetScpNumber)
	{
		ScpSpawner.EnqueuedScps.Clear();
		for (int i = 0; i < targetScpNumber; i++)
		{
			ScpSpawner.EnqueuedScps.Add(ScpSpawner.NextScp);
		}
		List<ReferenceHub> chosenPlayers = ScpPlayerPicker.ChoosePlayers(targetScpNumber);
		while (ScpSpawner.EnqueuedScps.Count > 0)
		{
			RoleTypeId scp = ScpSpawner.EnqueuedScps[0];
			ScpSpawner.EnqueuedScps.RemoveAt(0);
			ScpSpawner.AssignScp(chosenPlayers, scp, ScpSpawner.EnqueuedScps);
		}
	}

	private static void AssignScp(List<ReferenceHub> chosenPlayers, RoleTypeId scp, List<RoleTypeId> otherScps)
	{
		ScpSpawner.ChancesBuffer.Clear();
		int num = 1;
		int num2 = 0;
		foreach (ReferenceHub chosenPlayer in chosenPlayers)
		{
			int num3 = ScpSpawner.GetPreferenceOfPlayer(chosenPlayer, scp);
			foreach (RoleTypeId otherScp in otherScps)
			{
				num3 -= ScpSpawner.GetPreferenceOfPlayer(chosenPlayer, otherScp);
			}
			num2++;
			ScpSpawner.ChancesBuffer[chosenPlayer] = num3;
			num = Mathf.Min(num3, num);
		}
		float num4 = 0f;
		ScpSpawner.SelectedSpawnChances.Clear();
		foreach (KeyValuePair<ReferenceHub, float> item in ScpSpawner.ChancesBuffer)
		{
			float num5 = Mathf.Pow(item.Value - (float)num + 1f, num2);
			ScpSpawner.SelectedSpawnChances[item.Key] = num5;
			num4 += num5;
		}
		float num6 = num4 * Random.value;
		float num7 = 0f;
		foreach (KeyValuePair<ReferenceHub, float> selectedSpawnChance in ScpSpawner.SelectedSpawnChances)
		{
			num7 += selectedSpawnChance.Value;
			if (!(num7 < num6))
			{
				ReferenceHub key = selectedSpawnChance.Key;
				chosenPlayers.Remove(key);
				key.roleManager.ServerSetRole(scp, RoleChangeReason.RoundStart);
				break;
			}
		}
	}

	private static int GetPreferenceOfPlayer(ReferenceHub ply, RoleTypeId scp)
	{
		int connectionId = ply.connectionToClient.connectionId;
		if (!ScpSpawnPreferences.Preferences.TryGetValue(connectionId, out var value))
		{
			return 0;
		}
		if (!value.Preferences.TryGetValue(scp, out var value2))
		{
			return 0;
		}
		return value2;
	}
}
