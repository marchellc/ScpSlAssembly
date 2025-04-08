using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class SingleBulletHitscan : HitscanHitregModuleBase
	{
		protected override void Fire()
		{
			Ray ray = base.RandomizeRay(base.ForwardRay, base.CurrentInaccuracy);
			float num;
			base.ServerPerformHitscan(ray, out num);
			this.SendHitmarker(num);
		}
	}
}
