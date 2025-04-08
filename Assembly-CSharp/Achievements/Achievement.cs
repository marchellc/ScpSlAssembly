using System;

namespace Achievements
{
	public readonly struct Achievement
	{
		public Achievement(string steamName, long discordId, bool byServer = false)
		{
			this.SteamName = steamName;
			this._discordId = discordId;
			this._steamProgress = string.Empty;
			this._maxValue = 0;
			this.ActivatedByServer = byServer;
		}

		public Achievement(string steamName, string steamParameter, long discordId, int maxValue, bool byServer = false)
		{
			this.SteamName = steamName;
			this._discordId = discordId;
			this._steamProgress = steamParameter;
			this._maxValue = maxValue;
			this.ActivatedByServer = byServer;
		}

		public void Achieve()
		{
		}

		public void AddProgress(int amt = 1)
		{
		}

		public void Reset()
		{
		}

		public readonly string SteamName;

		private readonly string _steamProgress;

		private readonly long _discordId;

		private readonly int _maxValue;

		public readonly bool ActivatedByServer;
	}
}
