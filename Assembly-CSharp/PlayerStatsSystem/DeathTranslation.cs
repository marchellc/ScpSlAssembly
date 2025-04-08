using System;

namespace PlayerStatsSystem
{
	public struct DeathTranslation
	{
		public DeathTranslation(byte id, int ragdoll, int deathscreen, string backup)
		{
			this.Id = id;
			this._ragdollTranId = ragdoll - 1;
			this._deathTranId = deathscreen - 1;
			this.LogLabel = backup;
			DeathTranslations.TranslationsById[id] = this;
		}

		private readonly int _ragdollTranId;

		private readonly int _deathTranId;

		public readonly byte Id;

		public readonly string LogLabel;
	}
}
