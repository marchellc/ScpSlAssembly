using System;
using Respawning.Waves;
using UnityEngine;

namespace Respawning.Graphics
{
	public abstract class SerializedWaveInterface : WaveInterfaceBase<SpawnableWaveBase>
	{
		protected override void Awake()
		{
			this.RefreshWave();
		}

		internal void RefreshWave()
		{
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				if (spawnableWaveBase.GetType().Name.Equals(this.WaveIdentifier, StringComparison.OrdinalIgnoreCase))
				{
					base.Wave = spawnableWaveBase;
					return;
				}
			}
			throw new NullReferenceException("Unable to find any Wave by the " + this.WaveIdentifier + " name.");
		}

		[HideInInspector]
		public string WaveIdentifier;
	}
}
