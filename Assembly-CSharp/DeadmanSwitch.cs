using System;
using Mirror;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using UnityEngine;

public static class DeadmanSwitch
{
	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			DeadmanSwitch._dmsTime = 240f;
			DeadmanSwitch._dmsDelay = 0f;
			DeadmanSwitch._dmsActive = false;
		};
		StaticUnityMethods.OnUpdate += DeadmanSwitch.OnUpdate;
	}

	private static void OnUpdate()
	{
		if (!NetworkServer.active || !AlphaWarheadController.SingletonSet)
		{
			return;
		}
		if (AlphaWarheadController.Singleton.AlreadyDetonated)
		{
			return;
		}
		if (DeadmanSwitch._dmsActive)
		{
			DeadmanSwitch.StartWarhead();
			return;
		}
		NtfSpawnWave ntfSpawnWave;
		ChaosSpawnWave chaosSpawnWave;
		if (!WaveManager.TryGet<NtfSpawnWave>(out ntfSpawnWave) || !WaveManager.TryGet<ChaosSpawnWave>(out chaosSpawnWave))
		{
			return;
		}
		ILimitedWave limitedWave = ntfSpawnWave;
		if (limitedWave != null)
		{
			ILimitedWave limitedWave2 = chaosSpawnWave;
			if (limitedWave2 != null)
			{
				if (limitedWave.RespawnTokens > 0 || limitedWave2.RespawnTokens > 0)
				{
					return;
				}
				DeadmanSwitch._dmsTime -= Time.deltaTime;
				if (DeadmanSwitch._dmsTime >= 0f)
				{
					return;
				}
				DeadmanSwitch.InitiateProtocol();
				return;
			}
		}
	}

	private static void InitiateProtocol()
	{
		string text = "BY ORDER OF O5 COMMAND . DEAD MAN SEQUENCE ACTIVATED";
		float num = NineTailedFoxAnnouncer.singleton.CalculateDuration(text, true, 1f) + 3f;
		RespawnEffectsController.ClearQueue();
		RespawnEffectsController.PlayCassieAnnouncement(text, true, true, true, "");
		DeadmanSwitch._dmsDelay = num + Time.time;
		DeadmanSwitch._dmsActive = true;
	}

	private static void StartWarhead()
	{
		if (DeadmanSwitch._dmsDelay > Time.time)
		{
			return;
		}
		AlphaWarheadController singleton = AlphaWarheadController.Singleton;
		AlphaWarheadSyncInfo info = singleton.Info;
		if (info.ScenarioType == WarheadScenarioType.DeadmanSwitch)
		{
			return;
		}
		info.ScenarioType = WarheadScenarioType.DeadmanSwitch;
		singleton.NetworkInfo = info;
		singleton.InstantPrepare();
		singleton.StartDetonation(false, true, null);
		singleton.IsLocked = true;
	}

	private const float DeadmanSwitchActivationTime = 240f;

	private const float AnnouncementTriggerDelay = 3f;

	private static float _dmsTime;

	private static float _dmsDelay;

	private static bool _dmsActive;
}
