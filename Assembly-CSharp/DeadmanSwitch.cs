using System;
using GameCore;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;
using Respawning;
using Respawning.Waves;
using Respawning.Waves.Generic;
using UnityEngine;

public static class DeadmanSwitch
{
	private const float DefaultActivationTime = 240f;

	private const float WaveSpawnActivationTime = 160f;

	private const float MiniWaveSpawnActivationDelay = 40f;

	private const float AnnouncementTriggerDelay = 3f;

	private const float ScpHealMaxHpPercentage = 0.25f;

	private static bool _forceCountdownToggle;

	private static float _dmsAnnouncementDelay;

	private static float _countdownTimer;

	public static bool ForceCountdownToggle
	{
		get
		{
			return DeadmanSwitch._forceCountdownToggle;
		}
		set
		{
			if (!value)
			{
				DeadmanSwitch.Reset();
			}
			DeadmanSwitch._forceCountdownToggle = value;
		}
	}

	public static bool IsSequenceActive { get; private set; }

	public static bool IsDeadmanSwitchEnabled { get; private set; }

	public static float CountdownTimeLeft
	{
		get
		{
			return DeadmanSwitch._countdownTimer;
		}
		set
		{
			DeadmanSwitch._countdownTimer = Mathf.Min(value, DeadmanSwitch.CountdownMaxTime);
		}
	}

	public static float CountdownMaxTime { get; set; }

	public static void Reset(bool resetSequence = true)
	{
		DeadmanSwitch._countdownTimer = ConfigFile.ServerConfig.GetFloat("dms_activation_time", 240f);
		if (resetSequence)
		{
			DeadmanSwitch.IsSequenceActive = false;
		}
	}

	public static void InitiateProtocol()
	{
		string text = "BY ORDER OF O5 COMMAND . DEAD MAN SEQUENCE ACTIVATED";
		float num = NineTailedFoxAnnouncer.singleton.CalculateDuration(text, rawNumber: true) + 3f;
		RespawnEffectsController.ClearQueue();
		RespawnEffectsController.PlayCassieAnnouncement(text, makeHold: true, makeNoise: true, customAnnouncement: true);
		DeadmanSwitch._dmsAnnouncementDelay = num + Time.time;
		DeadmanSwitch.IsSequenceActive = true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			DeadmanSwitch.ReloadConfigs();
			DeadmanSwitch._forceCountdownToggle = false;
			DeadmanSwitch._dmsAnnouncementDelay = 0f;
			DeadmanSwitch.IsSequenceActive = false;
		};
		StaticUnityMethods.OnUpdate += OnUpdate;
		WaveManager.OnWaveTrigger += OnWaveSpawned;
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(ReloadConfigs));
		AlphaWarheadController.OnDetonated += OnActivationHeal;
	}

	private static void OnWaveSpawned(SpawnableWaveBase wave)
	{
		if (wave is IMiniWave)
		{
			DeadmanSwitch.CountdownTimeLeft += 40f;
		}
		else if (!DeadmanSwitch.IsSequenceActive && !(DeadmanSwitch.CountdownTimeLeft >= 160f))
		{
			DeadmanSwitch.CountdownTimeLeft = 160f;
		}
	}

	private static void ReloadConfigs()
	{
		DeadmanSwitch.IsDeadmanSwitchEnabled = ConfigFile.ServerConfig.GetBool("dms_enabled", def: true);
		DeadmanSwitch.CountdownMaxTime = ConfigFile.ServerConfig.GetFloat("dms_activation_time", 240f);
		DeadmanSwitch.Reset(resetSequence: false);
	}

	private static void OnUpdate()
	{
		if (!NetworkServer.active || !AlphaWarheadController.SingletonSet || AlphaWarheadController.Singleton.AlreadyDetonated)
		{
			return;
		}
		if (DeadmanSwitch.IsSequenceActive)
		{
			DeadmanSwitch.StartWarhead();
			return;
		}
		if (!DeadmanSwitch.ForceCountdownToggle)
		{
			if (!DeadmanSwitch.IsDeadmanSwitchEnabled || !WaveManager.TryGet<NtfSpawnWave>(out var spawnWave) || !WaveManager.TryGet<ChaosSpawnWave>(out var spawnWave2))
			{
				return;
			}
			ILimitedWave limitedWave = spawnWave;
			if (limitedWave == null)
			{
				return;
			}
			ILimitedWave limitedWave2 = spawnWave2;
			if (limitedWave2 == null || limitedWave.RespawnTokens > 0 || limitedWave2.RespawnTokens > 0)
			{
				return;
			}
		}
		DeadmanSwitch._countdownTimer -= Time.deltaTime;
		if (!(DeadmanSwitch._countdownTimer >= 0f))
		{
			DeadmanSwitch.InitiateProtocol();
		}
	}

	private static void StartWarhead()
	{
		if (!(DeadmanSwitch._dmsAnnouncementDelay > Time.time))
		{
			AlphaWarheadController singleton = AlphaWarheadController.Singleton;
			AlphaWarheadSyncInfo info = singleton.Info;
			if (info.ScenarioType != WarheadScenarioType.DeadmanSwitch)
			{
				info.ScenarioType = WarheadScenarioType.DeadmanSwitch;
				singleton.NetworkInfo = info;
				DeadmanSwitch.OnActivationHeal();
				singleton.InstantPrepare();
				singleton.StartDetonation(isAutomatic: false, suppressSubtitles: true);
				singleton.IsLocked = true;
			}
		}
	}

	private static void OnActivationHeal()
	{
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			PlayerRoleBase currentRole = allHub.roleManager.CurrentRole;
			if (currentRole.Team == Team.SCPs && currentRole.RoleTypeId != RoleTypeId.Scp0492 && currentRole is IHealthbarRole)
			{
				HealthStat module = allHub.playerStats.GetModule<HealthStat>();
				float num = module.MaxValue * 0.25f;
				if (!(module.CurValue >= num))
				{
					module.CurValue = num;
				}
			}
		}
	}
}
