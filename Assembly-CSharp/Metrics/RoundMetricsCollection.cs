using System;
using System.Collections.Generic;
using System.Linq;

namespace Metrics
{
	public struct RoundMetricsCollection : IJsonSerializable
	{
		public RoundMetricsCollection(IEnumerable<CollectedMetric> metrics)
		{
			this.Metrics = metrics.ToArray<CollectedMetric>();
		}

		public CollectedMetric[] Metrics;
	}
}
