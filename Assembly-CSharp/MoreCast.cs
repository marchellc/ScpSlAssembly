using System;
using System.Collections.Generic;
using UnityEngine;

public class MoreCast : MonoBehaviour
{
	public static bool BeamCast(Vector3 start, Vector3 end, Vector3 beamRadius, float beamStep, out List<RaycastHit> hitInfo, int layerMask, bool any)
	{
		hitInfo = new List<RaycastHit>();
		Vector3 vector = start;
		Vector3 vector2 = end;
		vector -= beamRadius;
		vector2 -= beamRadius;
		for (float num = -beamRadius.x; num < beamRadius.x; num += beamStep)
		{
			vector.y = start.y;
			vector2.y = end.y;
			vector.x += beamStep;
			vector2.x += beamStep;
			for (float num2 = -beamRadius.y; num2 < beamRadius.x; num2 += beamStep)
			{
				vector.z = start.z;
				vector2.z = end.z;
				vector.y += beamStep;
				vector2.y += beamStep;
				for (float num3 = -beamRadius.x; num3 < beamRadius.x; num3 += beamStep)
				{
					vector.z += beamStep;
					vector2.z += beamStep;
					RaycastHit raycastHit;
					bool flag = Physics.Linecast(vector, vector2, out raycastHit, layerMask);
					hitInfo.Add(raycastHit);
					if (any && flag)
					{
						return true;
					}
					if (!flag && !any)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	public static bool BeamCast(Vector3 start, Vector3 end, Vector3 beamRadius, float beamStep, int layerMask, bool any)
	{
		List<RaycastHit> list;
		return MoreCast.BeamCast(start, end, beamRadius, beamStep, out list, layerMask, any);
	}
}
