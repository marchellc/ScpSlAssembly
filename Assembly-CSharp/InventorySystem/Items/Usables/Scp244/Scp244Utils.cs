using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244
{
	public static class Scp244Utils
	{
		public static bool CheckVisibility(Vector3 observer, Vector3 target)
		{
			if ((observer - target).sqrMagnitude <= 73.96f)
			{
				return true;
			}
			using (HashSet<Scp244DeployablePickup>.Enumerator enumerator = Scp244DeployablePickup.Instances.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IntersectRay(observer, target))
					{
						return false;
					}
				}
			}
			return true;
		}

		public static bool IntersectRay(this Scp244DeployablePickup scp244, Vector3 observer, Vector3 target)
		{
			if (scp244.State == Scp244State.Idle || scp244.CurrentSizePercent < 0.55f)
			{
				return false;
			}
			Vector3 vector = target - observer;
			float magnitude = vector.magnitude;
			Vector3 vector2 = vector / magnitude;
			Ray ray = new Ray(observer, vector2);
			float num;
			if (!scp244.CurrentBounds.IntersectRay(ray, out num) || num > magnitude)
			{
				return false;
			}
			Vector3 position = scp244.transform.position;
			float num2 = scp244.CurrentDiameter / 2f;
			float num3 = Vector3.Dot(position - observer, vector2);
			Vector3 vector3 = observer + vector2 * Mathf.Clamp(num3, 0f, magnitude);
			return (position - vector3).sqrMagnitude < num2 * num2;
		}
	}
}
