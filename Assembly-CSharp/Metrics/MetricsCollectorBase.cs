using System;

namespace Metrics
{
	public abstract class MetricsCollectorBase : IJsonSerializable
	{
		public virtual void OnRoundStarted()
		{
		}

		public virtual void OnRoundEnded(RoundSummary.LeadingTeam winningTeam)
		{
		}

		public virtual void Init()
		{
		}

		protected void RecordData<T>(T data, bool checkIfRoundActive = true) where T : MetricsCollectorBase
		{
			MetricsAggregator.RecordData<T>(data, checkIfRoundActive);
		}
	}
}
