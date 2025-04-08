using System;
using Mirror;
using PlayerStatsSystem;

namespace Metrics
{
	public class DeathsCollector : MetricsCollectorBase
	{
		public override void Init()
		{
			base.Init();
			PlayerStats.OnAnyPlayerDied += this.OnAnyPlayerDied;
		}

		private void OnAnyPlayerDied(ReferenceHub deadPlayer, DamageHandlerBase damageHandler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.RecordData<DeathsCollector>(new DeathsCollector
			{
				VictimLifeId = deadPlayer.roleManager.CurrentRole.UniqueLifeIdentifier,
				HandlerType = damageHandler.GetType().Name,
				ExtraData = damageHandler.ServerMetricsText
			}, true);
		}

		public int VictimLifeId;

		public string HandlerType;

		public string ExtraData;
	}
}
