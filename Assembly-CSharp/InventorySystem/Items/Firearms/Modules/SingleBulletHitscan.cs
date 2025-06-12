using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class SingleBulletHitscan : HitscanHitregModuleBase
{
	protected override void Fire()
	{
		Ray targetRay = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
		this.ServerApplyDamage(base.ServerPrescan(targetRay));
	}
}
