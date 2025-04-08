using System;
using System.Collections.Generic;
using System.IO;
using GameCore;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;
using Utf8Json;
using Utils.NonAllocLINQ;

namespace Metrics
{
	public static class MetricsAggregator
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			MetricsAggregator.Collectors.ForEach(delegate(MetricsCollectorBase x)
			{
				x.Init();
			});
			RoundSummary.OnRoundEnded += MetricsAggregator.OnRoundEnded;
			CharacterClassManager.ServerOnRoundStartTriggered += MetricsAggregator.OnRoundStarted;
			MetricsAggregator.ReloadConfig();
			ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(MetricsAggregator.ReloadConfig));
		}

		private static void ReloadConfig()
		{
			MetricsAggregator._metricsSavingEnabled = ConfigFile.ServerConfig.GetBool("save_metrics", true);
		}

		private static void OnRoundStarted()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			MetricsAggregator._roundActive = true;
			MetricsAggregator.Collectors.ForEach(delegate(MetricsCollectorBase x)
			{
				x.OnRoundStarted();
			});
		}

		private static void OnRoundEnded(RoundSummary.LeadingTeam winner, RoundSummary.SumInfo_ClassList sumInfo)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			MetricsAggregator._roundActive = false;
			foreach (MetricsCollectorBase metricsCollectorBase in MetricsAggregator.Collectors)
			{
				metricsCollectorBase.OnRoundEnded(winner);
			}
			if (!MetricsAggregator._metricsSavingEnabled)
			{
				return;
			}
			try
			{
				ushort port = LiteNetLib4MirrorTransport.Singleton.port;
				string text = string.Format("{0}Metrics/{1}", FileManager.GetAppFolder(true, false, ""), port);
				Directory.CreateDirectory(text);
				string text2 = TimeBehaviour.FormatTime("yyyy-MM-dd HH.mm.ss");
				using (StreamWriter streamWriter = new StreamWriter(text + "/Round " + text2 + ".json"))
				{
					RoundMetricsCollection roundMetricsCollection = new RoundMetricsCollection(MetricsAggregator.CollectedMetrics);
					streamWriter.Write(JsonSerializer.ToJsonString<RoundMetricsCollection>(roundMetricsCollection));
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		public static void RecordData<T>(T data, bool checkIfRoundActive) where T : MetricsCollectorBase
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (checkIfRoundActive && !MetricsAggregator._roundActive)
			{
				return;
			}
			if (data == null)
			{
				throw new ArgumentNullException("data", "Attempting to record a null metric.");
			}
			if (MetricsAggregator.Collectors.Contains(data))
			{
				throw new ArgumentException("Cannot record the static collector definition. Create a new instance to record data.");
			}
			string text = JsonSerializer.ToJsonString<T>(data);
			MetricsAggregator.CollectedMetrics.Add(new CollectedMetric(typeof(T), text));
		}

		private static readonly HashSet<MetricsCollectorBase> Collectors = new HashSet<MetricsCollectorBase>
		{
			new DeathsCollector(),
			new RoleChangeCollector(),
			new RoundSummaryCollector(),
			new ScpPreferencesCollector()
		};

		private const string SaveConfigKey = "save_metrics";

		private const bool SaveConfigDefault = true;

		private static bool _roundActive;

		private static bool _metricsSavingEnabled;

		private static readonly List<CollectedMetric> CollectedMetrics = new List<CollectedMetric>();
	}
}
