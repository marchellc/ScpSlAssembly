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
			return _forceCountdownToggle;
		}
		set
		{
			if (!value)
			{
				Reset();
			}
			_forceCountdownToggle = value;
		}
	}

	public static bool IsSequenceActive { get; private set; }

	public static bool IsDeadmanSwitchEnabled { get; private set; }

	public static float CountdownTimeLeft
	{
		get
		{
			return _countdownTimer;
		}
		set
		{
			_countdownTimer = Mathf.Min(value, CountdownMaxTime);
		}
	}

	public static float CountdownMaxTime { get; set; }

	public static void Reset(bool resetSequence = true)
	{
		_countdownTimer = ConfigFile.ServerConfig.GetFloat("dms_activation_time", 240f);
		if (resetSequence)
		{
			IsSequenceActive = false;
		}
	}

	public static void InitiateProtocol()
	{
		string text = "BY ORDER OF O5 COMMAND . DEAD MAN SEQUENCE ACTIVATED";
		float num = NineTailedFoxAnnouncer.singleton.CalculateDuration(text, rawNumber: true) + 3f;
		RespawnEffectsController.ClearQueue();
		RespawnEffectsController.PlayCassieAnnouncement(text, makeHold: true, makeNoise: true, customAnnouncement: true);
		_dmsAnnouncementDelay = num + Time.time;
		IsSequenceActive = true;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			ReloadConfigs();
			_forceCountdownToggle = false;
			_dmsAnnouncementDelay = 0f;
			IsSequenceActive = false;
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
			CountdownTimeLeft += 40f;
		}
		else if (!IsSequenceActive && !(CountdownTimeLeft >= 160f))
		{
			CountdownTimeLeft = 160f;
		}
	}

	private static void ReloadConfigs()
	{
		IsDeadmanSwitchEnabled = ConfigFile.ServerConfig.GetBool("dms_enabled", def: true);
		CountdownMaxTime = ConfigFile.ServerConfig.GetFloat("dms_activation_time", 240f);
		Reset(resetSequence: false);
	}

	private static void OnUpdate()
	{
		if (!NetworkServer.active || !AlphaWarheadController.SingletonSet || AlphaWarheadController.Singleton.AlreadyDetonated)
		{
			return;
		}
		if (IsSequenceActive)
		{
			StartWarhead();
			return;
		}
		if (!ForceCountdownToggle)
		{
			if (!IsDeadmanSwitchEnabled || !WaveManager.TryGet<NtfSpawnWave>(out var spawnWave) || !WaveManager.TryGet<ChaosSpawnWave>(out var spawnWave2))
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
		_countdownTimer -= Time.deltaTime;
		if (!(_countdownTimer >= 0f))
		{
			InitiateProtocol();
		}
	}

	private static void StartWarhead()
	{
		if (!(_dmsAnnouncementDelay > Time.time))
		{
			AlphaWarheadController singleton = AlphaWarheadController.Singleton;
			AlphaWarheadSyncInfo info = singleton.Info;
			if (info.ScenarioType != WarheadScenarioType.DeadmanSwitch)
			{
				info.ScenarioType = WarheadScenarioType.DeadmanSwitch;
				singleton.NetworkInfo = info;
				OnActivationHeal();
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
