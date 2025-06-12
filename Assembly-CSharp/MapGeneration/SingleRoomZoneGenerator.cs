using System;
using MapGeneration.Holidays;
using UnityEngine;

namespace MapGeneration;

public class SingleRoomZoneGenerator : ZoneGenerator
{
	[SerializeField]
	private Pose _spawnPosition;

	public Transform ZoneSpawnParent;

	public SpawnableRoom Prefab;

	public override void Generate(System.Random rng)
	{
		if (!this.Prefab.HolidayVariants.TryGetResult<HolidayRoomVariant, SpawnableRoom>(out var result))
		{
			result = this.Prefab;
		}
		result.RegisterIdentities();
		UnityEngine.Object.Instantiate(result, this._spawnPosition.position, this._spawnPosition.rotation, this.ZoneSpawnParent).SetupNetIdHandlers(0);
	}
}
