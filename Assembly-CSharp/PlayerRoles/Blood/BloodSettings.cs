using System;
using Decals;
using UnityEngine;

namespace PlayerRoles.Blood
{
	[Serializable]
	public class BloodSettings
	{
		public DecalPoolType Decal { get; private set; }

		public float DecalSpawnChance { get; private set; }

		public bool RandomDecalValidate
		{
			get
			{
				return this.GetRandom(this.DecalSpawnChance);
			}
		}

		private bool GetRandom(float chance)
		{
			return chance > 0f && (chance >= 1f || global::UnityEngine.Random.value <= chance);
		}
	}
}
