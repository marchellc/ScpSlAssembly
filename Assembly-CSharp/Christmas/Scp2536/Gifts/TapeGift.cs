using System;
using System.Diagnostics;
using CentralAuth;
using GameCore;
using InventorySystem;
using InventorySystem.Items;
using MapGeneration;
using PlayerRoles;
using Respawning.Waves;
using UnityEngine;

namespace Christmas.Scp2536.Gifts;

public class TapeGift : Scp2536GiftBase
{
	private const float BaseChances = 10f;

	private const float ChanceIncrementPerFail = 15f;

	private static float _spawnChances = 10f;

	private static bool _canSpawn;

	private static readonly Stopwatch LastGiven = Stopwatch.StartNew();

	public override UrgencyLevel Urgency => UrgencyLevel.Zero;

	public override void ServerGrant(ReferenceHub hub)
	{
		LastGiven.Restart();
		hub.inventory.ServerAddItem(ItemType.SCP1507Tape, ItemAddReason.Scp2536, 0);
		_spawnChances = 10f;
	}

	public override bool CanBeGranted(ReferenceHub hub)
	{
		if (!_canSpawn)
		{
			return false;
		}
		if (RoundStart.RoundLength.TotalMinutes < 8.0 || LastGiven.Elapsed.TotalMinutes < 3.0)
		{
			return false;
		}
		int count = WaveSpawner.GetAvailablePlayers(Team.Flamingos).Count;
		int playerCount = ReferenceHub.GetPlayerCount(ClientInstanceMode.ReadyClient, ClientInstanceMode.Dummy);
		return (float)count >= (float)playerCount * 0.5f;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationFinished += delegate
		{
			double num = new System.Random(SeedSynchronizer.Seed).NextDouble() * 100.0;
			_canSpawn = (double)_spawnChances >= num;
			if (!_canSpawn)
			{
				_spawnChances += 15f;
			}
		};
	}
}
