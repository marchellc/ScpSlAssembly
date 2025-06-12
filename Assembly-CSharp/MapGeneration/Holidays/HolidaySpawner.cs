using Mirror;
using UnityEngine;

namespace MapGeneration.Holidays;

public class HolidaySpawner : MonoBehaviour
{
	public HolidaySpawnable[] Spawnables;

	private void Awake()
	{
		HolidaySpawnable[] spawnables = this.Spawnables;
		foreach (HolidaySpawnable holidaySpawnable in spawnables)
		{
			if (holidaySpawnable.AvailableHolidays.IsAnyHolidayActive(mustBeForced: true))
			{
				NetworkClient.RegisterPrefab(holidaySpawnable.gameObject);
				if (NetworkServer.active)
				{
					NetworkServer.Spawn(Object.Instantiate(holidaySpawnable.gameObject));
				}
			}
		}
	}
}
