using System;
using System.Collections.Generic;
using GameCore;
using MapGeneration;
using MapGeneration.Holidays;
using UnityEngine;

public class ClutterSpawner : MonoBehaviour
{
	private void Awake()
	{
		SeedSynchronizer.OnGenerationStage += this.OnMapStage;
	}

	private void OnDestroy()
	{
		SeedSynchronizer.OnGenerationStage -= this.OnMapStage;
	}

	private void Start()
	{
		ClutterSpawner.noHolidays = ConfigFile.ServerConfig.GetBool("no_holidays", false);
	}

	private void OnMapStage(MapGenerationPhase stage)
	{
		if (stage != MapGenerationPhase.ComplexDecorationsAndClutter)
		{
			return;
		}
		this.GenerateClutter();
	}

	public void GenerateClutter()
	{
		bool flag = false;
		for (int i = this.clutters.Count - 1; i >= 0; i--)
		{
			ClutterStruct clutterStruct = this.clutters[i];
			global::GameCore.Console.AddDebugLog("MGCLTR", string.Concat(new string[]
			{
				"Checking spawn conditions for clutter struct \"",
				clutterStruct.descriptor,
				"\" on object \"",
				base.gameObject.name,
				"\""
			}), MessageImportance.LeastImportant, true);
			if (clutterStruct.clutterComponent && !clutterStruct.clutterComponent.spawned)
			{
				bool flag2;
				if (clutterStruct.chanceToSpawn <= 0f)
				{
					flag2 = false;
				}
				else if ((float)global::UnityEngine.Random.Range(1, 101) > clutterStruct.chanceToSpawn)
				{
					flag2 = false;
				}
				else
				{
					bool invertTimespan = clutterStruct.invertTimespan;
					bool flag3 = clutterStruct.targetHolidays.IsAnyHolidayActive(false, false);
					flag2 = (invertTimespan ? (!flag3) : flag3);
				}
				if (flag2 && (!this.IsExclusive || !flag))
				{
					clutterStruct.clutterComponent.SpawnClutter();
					flag = true;
				}
				else
				{
					clutterStruct.clutterComponent.gameObject.SetActive(false);
					global::UnityEngine.Object.Destroy(clutterStruct.clutterComponent.holderObject);
				}
			}
		}
	}

	[SerializeField]
	private bool IsExclusive;

	[SerializeField]
	private List<ClutterStruct> clutters = new List<ClutterStruct>();

	private static bool noHolidays;
}
