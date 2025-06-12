using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public readonly struct DestructibleHitPair
{
	public readonly IDestructible Destructible;

	public readonly HitRayPair Raycast;

	public Ray Ray => this.Raycast.Ray;

	public RaycastHit Hit => this.Raycast.Hit;

	public DestructibleHitPair(IDestructible destructible, RaycastHit hit, Ray ray)
	{
		this.Destructible = destructible;
		this.Raycast = new HitRayPair(ray, hit);
	}
}
