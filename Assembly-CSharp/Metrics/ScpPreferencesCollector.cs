using System;
using System.Collections.Generic;
using PlayerRoles;
using PlayerRoles.RoleAssign;

namespace Metrics
{
	public class ScpPreferencesCollector : MetricsCollectorBase
	{
		public override void OnRoundStarted()
		{
			base.OnRoundStarted();
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				ScpSpawnPreferences.SpawnPreferences spawnPreferences;
				if (ScpSpawnPreferences.Preferences.TryGetValue(referenceHub.connectionToClient.connectionId, out spawnPreferences))
				{
					this.RecordSpawnPreferences(spawnPreferences);
				}
			}
		}

		private void RecordSpawnPreferences(ScpSpawnPreferences.SpawnPreferences sp)
		{
			ScpPreferencesCollector.PrefPair[] array = new ScpPreferencesCollector.PrefPair[sp.Preferences.Count];
			int num = 0;
			foreach (KeyValuePair<RoleTypeId, int> keyValuePair in sp.Preferences)
			{
				array[num++] = new ScpPreferencesCollector.PrefPair(keyValuePair);
			}
			base.RecordData<ScpPreferencesCollector>(new ScpPreferencesCollector
			{
				UserPreferences = array
			}, true);
		}

		public ScpPreferencesCollector.PrefPair[] UserPreferences;

		[Serializable]
		public struct PrefPair
		{
			public PrefPair(KeyValuePair<RoleTypeId, int> kvp)
			{
				this.Role = kvp.Key;
				this.Weight = kvp.Value;
			}

			public RoleTypeId Role;

			public int Weight;
		}
	}
}
