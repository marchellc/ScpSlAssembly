using System;
using System.Runtime.CompilerServices;
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

namespace PlayerRoles.PlayableScps.Scp1507
{
	public static class Scp1507Spawner
	{
		public static Scp1507Spawner.State CurState { get; private set; }

		private static bool Inactive
		{
			get
			{
				return Scp1507Spawner.CurState == Scp1507Spawner.State.Idle || Scp1507Spawner.CurState == Scp1507Spawner.State.Spawned;
			}
		}

		public static void StartSpawning(ReferenceHub newAlpha)
		{
			Scp1507Spawner._alpha = newAlpha;
			if (!Scp1507Spawner.Inactive)
			{
				return;
			}
			Scp1507Spawner._elapsed = 0f;
			StaticUnityMethods.OnUpdate += Scp1507Spawner.Update;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(Scp1507Spawner.OnPlayerRemoved));
			Scp1507Spawner.CurState = Scp1507Spawner.State.WaitForRespawnCycle;
		}

		public static void Restore()
		{
			if (!Scp1507Spawner.Inactive)
			{
				StaticUnityMethods.OnUpdate -= Scp1507Spawner.Update;
				ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(Scp1507Spawner.OnPlayerRemoved));
			}
			Scp1507Spawner.CurState = Scp1507Spawner.State.Idle;
		}

		private static void Spawn()
		{
			Scp1507Spawner.Restore();
			Scp1507Spawner.CurState = Scp1507Spawner.State.Spawned;
			Scp1507Spawner._alpha.inventory.ServerDropEverything();
			Scp1507Spawner.<Spawn>g__SpawnPlayer|17_0(Scp1507Spawner._alpha, RoleTypeId.AlphaFlamingo);
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (Scp1507Spawner.ValidatePlayer(referenceHub))
				{
					Scp1507Spawner.<Spawn>g__SpawnPlayer|17_0(referenceHub, RoleTypeId.Flamingo);
				}
			}
			ElevatorChamber elevatorChamber;
			if (!ElevatorChamber.TryGetChamber(ElevatorGroup.Nuke01, out elevatorChamber))
			{
				return;
			}
			elevatorChamber.ServerSetDestination(1, true);
			ElevatorChamber elevatorChamber2;
			if (!ElevatorChamber.TryGetChamber(ElevatorGroup.Nuke02, out elevatorChamber2))
			{
				return;
			}
			elevatorChamber2.ServerSetDestination(1, true);
		}

		private static void HandleEscapeDoor()
		{
			DoorNametagExtension doorNametagExtension;
			if (!DoorNametagExtension.NamedDoors.TryGetValue("ESCAPE_FINAL", out doorNametagExtension))
			{
				return;
			}
			doorNametagExtension.TargetDoor.NetworkTargetState = true;
		}

		private static bool ValidatePlayer(ReferenceHub candidate)
		{
			SpectatorRole spectatorRole = candidate.roleManager.CurrentRole as SpectatorRole;
			return spectatorRole != null && spectatorRole.ReadyToRespawn && candidate != Scp1507Spawner._alpha;
		}

		private static void OnPlayerRemoved(ReferenceHub hub)
		{
			if (hub != Scp1507Spawner._alpha)
			{
				return;
			}
			Scp1507Spawner.Restore();
		}

		private static void Update()
		{
			Scp1507Spawner._elapsed += Time.deltaTime;
			switch (Scp1507Spawner.CurState)
			{
			case Scp1507Spawner.State.WaitForRespawnCycle:
				Scp1507Spawner.UpdateWaitForRespawnCycle();
				return;
			case Scp1507Spawner.State.WaitForSpectators:
				Scp1507Spawner.UpdateWaitForSpectators();
				return;
			case Scp1507Spawner.State.Spawning:
				Scp1507Spawner.UpdateSpawning();
				return;
			default:
				return;
			}
		}

		private static void UpdateWaitForRespawnCycle()
		{
			if (WaveManager.State != WaveQueueState.Idle)
			{
				return;
			}
			Scp1507Spawner.CurState = Scp1507Spawner.State.WaitForSpectators;
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				TimeBasedWave timeBasedWave = (TimeBasedWave)spawnableWaveBase;
				float num = 240f;
				if (timeBasedWave is IMiniWave)
				{
					num *= 0.5f;
				}
				timeBasedWave.Timer.SpawnIntervalSeconds = num;
				timeBasedWave.Timer.Reset(true);
			}
		}

		private static void UpdateWaitForSpectators()
		{
			if (Scp1507Spawner._elapsed < 7f)
			{
				return;
			}
			float num = (float)ReferenceHub.AllHubs.Count(new Func<ReferenceHub, bool>(Scp1507Spawner.ValidatePlayer));
			float num2 = (float)(ReferenceHub.AllHubs.Count - 2);
			if (((num2 <= 0f) ? 1f : (num / num2)) < 0.3f && Scp1507Spawner._elapsed < 180f)
			{
				return;
			}
			Scp1507Spawner._elapsed = 0f;
			Scp1507Spawner.CurState = Scp1507Spawner.State.Spawning;
			Scp1507Spawner._alpha.playerEffectsController.EnableEffect<BecomingFlamingo>(0f, false);
		}

		private static void UpdateSpawning()
		{
			if (Scp1507Spawner._elapsed < 5f)
			{
				return;
			}
			Scp1507Spawner.Spawn();
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			RoundRestart.OnRestartTriggered += Scp1507Spawner.Restore;
		}

		[CompilerGenerated]
		internal static void <Spawn>g__SpawnPlayer|17_0(ReferenceHub hub, RoleTypeId role)
		{
			if (AlphaWarheadController.Detonated)
			{
				hub.roleManager.ServerSetRole(role, RoleChangeReason.ItemUsage, ~RoleSpawnFlags.UseSpawnpoint);
				hub.TryOverridePosition(Scp1507Spawner.PostDetonationSpawnpoint);
				hub.TryOverrideRotation(Vector3.zero);
				Scp1507Spawner.HandleEscapeDoor();
				return;
			}
			hub.roleManager.ServerSetRole(role, RoleChangeReason.ItemUsage, RoleSpawnFlags.All);
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

		public enum State
		{
			Idle,
			WaitForRespawnCycle,
			WaitForSpectators,
			Spawning,
			Spawned
		}
	}
}
