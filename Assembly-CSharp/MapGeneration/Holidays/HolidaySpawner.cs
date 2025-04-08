using System;
using Mirror;
using UnityEngine;

namespace MapGeneration.Holidays
{
	public class HolidaySpawner : MonoBehaviour
	{
		private void Awake()
		{
			foreach (HolidaySpawnable holidaySpawnable in this.Spawnables)
			{
				if (holidaySpawnable.AvailableHolidays.IsAnyHolidayActive(true, false))
				{
					NetworkClient.RegisterPrefab(holidaySpawnable.gameObject);
					if (NetworkServer.active)
					{
						NetworkServer.Spawn(global::UnityEngine.Object.Instantiate<GameObject>(holidaySpawnable.gameObject), null);
					}
				}
			}
		}

		public HolidaySpawnable[] Spawnables;
	}
}
