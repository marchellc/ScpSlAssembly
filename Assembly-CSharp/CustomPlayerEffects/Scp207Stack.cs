using System;

namespace CustomPlayerEffects
{
	[Serializable]
	public struct Scp207Stack : ICokeStack
	{
		public float PostProcessIntensity { readonly get; set; }

		public float SpeedMultiplier { readonly get; set; }

		public float DamageAmount;
	}
}
