using System;

namespace Metrics
{
	public class RoundSummaryCollector : MetricsCollectorBase
	{
		public override void OnRoundEnded(RoundSummary.LeadingTeam winningTeam)
		{
			base.OnRoundEnded(winningTeam);
			base.RecordData<RoundSummaryCollector>(new RoundSummaryCollector
			{
				WonTeam = winningTeam
			}, false);
		}

		public RoundSummary.LeadingTeam WonTeam;
	}
}
