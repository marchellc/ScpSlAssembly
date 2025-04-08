using System;
using System.Linq;
using GameCore;
using Mirror;
using UnityEngine;

public static class PocketDimensionGenerator
{
	public static void RandomizeTeleports()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		global::System.Random random = Misc.CreateRandom();
		PocketDimensionTeleport[] array = PocketDimensionGenerator.PrepTeleports();
		int @int = ConfigFile.ServerConfig.GetInt("pd_exit_count", 2);
		int num = 0;
		while (num < @int && PocketDimensionGenerator.ContainsKiller(array))
		{
			int num2 = -1;
			while ((num2 < 0 || array[num2].GetTeleportType() == PocketDimensionTeleport.PDTeleportType.Exit) && PocketDimensionGenerator.ContainsKiller(array))
			{
				num2 = random.Next(0, array.Length);
			}
			array[Mathf.Clamp(num2, 0, array.Length - 1)].SetType(PocketDimensionTeleport.PDTeleportType.Exit);
			num++;
		}
	}

	private static PocketDimensionTeleport[] PrepTeleports()
	{
		PocketDimensionTeleport[] array = PocketDimensionTeleport.AllInstances.ToArray<PocketDimensionTeleport>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetType(PocketDimensionTeleport.PDTeleportType.Killer);
		}
		return array;
	}

	private static bool ContainsKiller(PocketDimensionTeleport[] pdtps)
	{
		for (int i = 0; i < pdtps.Length; i++)
		{
			if (pdtps[i].GetTeleportType() == PocketDimensionTeleport.PDTeleportType.Killer)
			{
				return true;
			}
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PocketDimensionTeleport.OnInstancesUpdated += PocketDimensionGenerator.RandomizeTeleports;
	}
}
