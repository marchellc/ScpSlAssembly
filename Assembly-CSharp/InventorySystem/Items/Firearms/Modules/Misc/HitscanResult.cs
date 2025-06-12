using System.Collections.Generic;
using PlayerStatsSystem;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class HitscanResult
{
	public readonly List<DestructibleHitPair> Destructibles = new List<DestructibleHitPair>();

	public readonly List<HitRayPair> Obstacles = new List<HitRayPair>();

	public readonly List<DestructibleDamageRecord> DamagedDestructibles = new List<DestructibleDamageRecord>();

	public float OtherDamage;

	public void Clear()
	{
		this.DamagedDestructibles.Clear();
		this.Destructibles.Clear();
		this.Obstacles.Clear();
		this.OtherDamage = 0f;
	}

	public void RegisterDamage(IDestructible dest, float appliedDamage, AttackerDamageHandler handler)
	{
		this.DamagedDestructibles.Add(new DestructibleDamageRecord(dest, appliedDamage, handler));
	}

	public float CountDamage(IDestructible dest)
	{
		float num = 0f;
		foreach (DestructibleDamageRecord damagedDestructible in this.DamagedDestructibles)
		{
			if (damagedDestructible.Destructible == dest)
			{
				num += damagedDestructible.AppliedDamage;
			}
		}
		return num;
	}
}
