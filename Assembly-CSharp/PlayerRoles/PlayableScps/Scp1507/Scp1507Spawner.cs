using CustomPlayerEffects;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using InventorySystem;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using Respawning;
using Respawning.Waves;
using RoundRestarting;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp1507;

public static class Scp1507Spawner
{
	public enum State
	{
		Idle,
		WaitForRespawnCycle,
		WaitForSpectators,
		Spawning,
		Spawned
	}

	private static ReferenceHub _alpha;

	private static float _elapsed;

	private const int SpawnWaveSetTimeSeconds = 240;

	private const float MiniwaveTimerMultiplier = 0.5f;

	private const float SpawnAnimDelaySeconds = 5f;

	private const float MinSpectatorsPercent = 0.3f;

	private const float MinSpawnDelaySeconds = 7f;

	private const float MaxSpawnDelaySeconds = 180f;

	private static readonly Vector3 PostDetonationSpawnpoint = new Vector3(124f, 988.85f, 27f);

	public static State CurState { get; private set; }

	private static bool Inactive
	{
		get
		{
			if (Scp1507Spawner.CurState != State.Idle)
			{
				return Scp1507Spawner.CurState == State.Spawned;
			}
			return true;
		}
	}

	public static void StartSpawning(ReferenceHub newAlpha)
	{
		Scp1507Spawner._alpha = newAlpha;
		if (Scp1507Spawner.Inactive)
		{
			Scp1507Spawner._elapsed = 0f;
			StaticUnityMethods.OnUpdate += Update;
			ReferenceHub.OnPlayerRemoved += OnPlayerRemoved;
			Scp1507Spawner.CurState = State.WaitForRespawnCycle;
		}
	}

	public static void Restore()
	{
		if (!Scp1507Spawner.Inactive)
		{
			StaticUnityMethods.OnUpdate -= Update;
			ReferenceHub.OnPlayerRemoved -= OnPlayerRemoved;
		}
		Scp1507Spawner.CurState = State.Idle;
	}

	private static void Spawn()
	{
		Scp1507Spawner.Restore();
		Scp1507Spawner.CurState = State.Spawned;
		Scp1507Spawner._alpha.inventory.ServerDropEverything();
		SpawnPlayer(Scp1507Spawner._alpha, RoleTypeId.AlphaFlamingo);
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (Scp1507Spawner.ValidatePlayer(allHub))
			{
				SpawnPlayer(allHub, RoleTypeId.Flamingo);
			}
		}
		if (ElevatorChamber.TryGetChamber(ElevatorGroup.Nuke01, out var chamber))
		{
			chamber.ServerSetDestination(1, allowQueueing: true);
			if (ElevatorChamber.TryGetChamber(ElevatorGroup.Nuke02, out var chamber2))
			{
				chamber2.ServerSetDestination(1, allowQueueing: true);
			}
		}
		static void SpawnPlayer(ReferenceHub hub, RoleTypeId role)
		{
			if (AlphaWarheadController.Detonated)
			{
				hub.roleManager.ServerSetRole(role, RoleChangeReason.ItemUsage, ~RoleSpawnFlags.UseSpawnpoint);
				hub.TryOverridePosition(Scp1507Spawner.PostDetonationSpawnpoint);
				hub.TryOverrideRotation(Vector3.zero);
				Scp1507Spawner.HandleEscapeDoor();
			}
			else
			{
				hub.roleManager.ServerSetRole(role, RoleChangeReason.ItemUsage);
			}
		}
	}

	private static void HandleEscapeDoor()
	{
		if (DoorNametagExtension.NamedDoors.TryGetValue("ESCAPE_FINAL", out var value))
		{
			value.TargetDoor.NetworkTargetState = true;
		}
	}

	private static bool ValidatePlayer(ReferenceHub candidate)
	{
		if (candidate.roleManager.CurrentRole is SpectatorRole { ReadyToRespawn: not false })
		{
			return candidate != Scp1507Spawner._alpha;
		}
		return false;
	}

	private static void OnPlayerRemoved(ReferenceHub hub)
	{
		if (!(hub != Scp1507Spawner._alpha))
		{
			Scp1507Spawner.Restore();
		}
	}

	private static void Update()
	{
		Scp1507Spawner._elapsed += Time.deltaTime;
		switch (Scp1507Spawner.CurState)
		{
		case State.WaitForRespawnCycle:
			Scp1507Spawner.UpdateWaitForRespawnCycle();
			break;
		case State.WaitForSpectators:
			Scp1507Spawner.UpdateWaitForSpectators();
			break;
		case State.Spawning:
			Scp1507Spawner.UpdateSpawning();
			break;
		}
	}

	private static void UpdateWaitForRespawnCycle()
	{
		if (WaveManager.State != WaveQueueState.Idle)
		{
			return;
		}
		Scp1507Spawner.CurState = State.WaitForSpectators;
		foreach (TimeBasedWave wave in WaveManager.Waves)
		{
			float num = 240f;
			if (wave is IMiniWave)
			{
				num *= 0.5f;
			}
			wave.Timer.SpawnIntervalSeconds = num;
			wave.Timer.Reset();
		}
	}

	private static void UpdateWaitForSpectators()
	{
		if (!(Scp1507Spawner._elapsed < 7f))
		{
			float num = ReferenceHub.AllHubs.Count(ValidatePlayer);
			float num2 = ReferenceHub.AllHubs.Count - 2;
			if (!(((num2 <= 0f) ? 1f : (num / num2)) < 0.3f) || !(Scp1507Spawner._elapsed < 180f))
			{
				Scp1507Spawner._elapsed = 0f;
				Scp1507Spawner.CurState = State.Spawning;
				Scp1507Spawner._alpha.playerEffectsController.EnableEffect<BecomingFlamingo>();
			}
		}
	}

	private static void UpdateSpawning()
	{
		if (!(Scp1507Spawner._elapsed < 5f))
		{
			Scp1507Spawner.Spawn();
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RoundRestart.OnRestartTriggered += Restore;
	}
}
