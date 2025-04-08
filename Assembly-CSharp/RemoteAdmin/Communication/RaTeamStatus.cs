using System;
using NorthwoodLib.Pools;
using Respawning;
using Respawning.Waves;

namespace RemoteAdmin.Communication
{
	public class RaTeamStatus : RaClientDataRequest
	{
		public override int DataId
		{
			get
			{
				return 8;
			}
		}

		protected override void GatherData()
		{
			StringBuilderPool.Shared.Rent();
			foreach (SpawnableWaveBase spawnableWaveBase in WaveManager.Waves)
			{
				base.AppendData(spawnableWaveBase.CreateDebugString().Replace(',', ';'));
			}
		}
	}
}
