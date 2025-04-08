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

namespace Christmas.Scp2536.Gifts
{
	public class TapeGift : Scp2536GiftBase
	{
		public override UrgencyLevel Urgency
		{
			get
			{
				return UrgencyLevel.Zero;
			}
		}

		public override void ServerGrant(ReferenceHub hub)
		{
			TapeGift.LastGiven.Restart();
			hub.inventory.ServerAddItem(ItemType.SCP1507Tape, ItemAddReason.Scp2536, 0, null);
			TapeGift._spawnChances = 10f;
		}

		public override bool CanBeGranted(ReferenceHub hub)
		{
			if (!TapeGift._canSpawn)
			{
				return false;
			}
			if (RoundStart.RoundLength.TotalMinutes < 8.0 || TapeGift.LastGiven.Elapsed.TotalMinutes < 3.0)
			{
				return false;
			}
			int count = WaveSpawner.GetAvailablePlayers(Team.Flamingos).Count;
			int playerCount = ReferenceHub.GetPlayerCount(new ClientInstanceMode[]
			{
				ClientInstanceMode.ReadyClient,
				ClientInstanceMode.Dummy
			});
			return (float)count >= (float)playerCount * 0.5f;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationFinished += delegate
			{
				double num = new global::System.Random(SeedSynchronizer.Seed).NextDouble() * 100.0;
				TapeGift._canSpawn = (double)TapeGift._spawnChances >= num;
				if (!TapeGift._canSpawn)
				{
					TapeGift._spawnChances += 15f;
				}
			};
		}

		private const float BaseChances = 10f;

		private const float ChanceIncrementPerFail = 15f;

		private static float _spawnChances = 10f;

		private static bool _canSpawn;

		private static readonly Stopwatch LastGiven = Stopwatch.StartNew();
	}
}
