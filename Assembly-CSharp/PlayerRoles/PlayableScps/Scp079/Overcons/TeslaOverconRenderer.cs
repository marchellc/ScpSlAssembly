using System;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public class TeslaOverconRenderer : PooledOverconRenderer
	{
		internal override void SpawnOvercons(Scp079Camera newCamera)
		{
			base.ReturnAll();
			foreach (TeslaGate teslaGate in TeslaGate.AllGates)
			{
				Vector3 position = teslaGate.transform.position;
				if (RoomUtils.IsTheSameRoom(newCamera.Position, position))
				{
					TeslaOvercon fromPool = base.GetFromPool<TeslaOvercon>();
					fromPool.transform.position = position + TeslaOverconRenderer.Offset;
					fromPool.Rescale(newCamera);
				}
			}
		}

		private static readonly Vector3 Offset = new Vector3(0f, 2.2f, 0f);
	}
}
