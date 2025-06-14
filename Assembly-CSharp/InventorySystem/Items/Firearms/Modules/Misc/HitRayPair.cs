using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public readonly struct HitRayPair
{
	public readonly Ray Ray;

	public readonly RaycastHit Hit;

	public HitRayPair(Ray ray, RaycastHit hit)
	{
		this.Ray = ray;
		this.Hit = hit;
	}
}
