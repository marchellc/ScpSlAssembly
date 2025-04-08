using System;
using MapGeneration.Holidays;
using UnityEngine;

namespace MapGeneration
{
	public class SurfaceZoneGenerator : ZoneGenerator
	{
		public override void Generate(global::System.Random rng)
		{
			SpawnableRoom surfacePrefab;
			if (!this.SurfacePrefab.HolidayVariants.TryGetResult(out surfacePrefab))
			{
				surfacePrefab = this.SurfacePrefab;
			}
			surfacePrefab.RegisterIdentities();
			global::UnityEngine.Object.Instantiate<SpawnableRoom>(surfacePrefab, this._spawnPosition.position, this._spawnPosition.rotation).SetupNetIdHandlers(0);
		}

		[SerializeField]
		private Pose _spawnPosition;

		public SpawnableRoom SurfacePrefab;
	}
}
