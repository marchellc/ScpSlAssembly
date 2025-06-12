using System;
using System.Collections.Generic;
using System.Linq;
using GameCore;

namespace Metrics;

public struct RoundMetricsCollection : IJsonSerializable
{
	public CollectedMetric[] Metrics;

	public string VersionString;

	public RoundMetricsCollection(IEnumerable<CollectedMetric> metrics)
	{
		this.Metrics = metrics.ToArray();
		this.VersionString = GameCore.Version.VersionString;
	}

	public void PrepBackwardsCompatibility()
	{
		if (!string.IsNullOrEmpty(this.VersionString))
		{
			return;
		}
		string name = typeof(RoundSummaryCollector).Name;
		for (int num = this.Metrics.Length - 2; num >= 0; num--)
		{
			if (!(this.Metrics[num].Type != name))
			{
				CollectedMetric[] array = new CollectedMetric[this.Metrics.Length - num - 1];
				Array.Copy(this.Metrics, num + 1, array, 0, array.Length);
				this.Metrics = array;
				break;
			}
		}
	}

	public readonly T[] GetOfType<T>() where T : MetricsCollectorBase
	{
		KeyValuePair<float, T>[] ofTypeWithTimestamp = this.GetOfTypeWithTimestamp<T>();
		T[] array = new T[ofTypeWithTimestamp.Length];
		for (int i = 0; i < ofTypeWithTimestamp.Length; i++)
		{
			array[i] = ofTypeWithTimestamp[i].Value;
		}
		return array;
	}

	public readonly KeyValuePair<float, T>[] GetOfTypeWithTimestamp<T>() where T : MetricsCollectorBase
	{
		List<KeyValuePair<float, T>> list = new List<KeyValuePair<float, T>>();
		string name = typeof(T).Name;
		CollectedMetric[] metrics = this.Metrics;
		for (int i = 0; i < metrics.Length; i++)
		{
			CollectedMetric collectedMetric = metrics[i];
			if (!(collectedMetric.Type != name))
			{
				float roundTime = collectedMetric.RoundTime;
				T value = JsonSerialize.FromJson<T>(collectedMetric.Data);
				list.Add(new KeyValuePair<float, T>(roundTime, value));
			}
		}
		return list.ToArray();
	}
}
