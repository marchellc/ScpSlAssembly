using System;
using UnityEngine;

namespace CameraShaking
{
	public class LookatShake : IShakeEffect
	{
		public LookatShake(Vector3 point)
		{
			this._point = point;
		}

		public bool GetEffect(ReferenceHub ply, out ShakeEffectValues shakeValues)
		{
			Transform playerCameraReference = ply.PlayerCameraReference;
			Vector3 eulerAngles = playerCameraReference.eulerAngles;
			Vector3 eulerAngles2 = Quaternion.LookRotation(this._point - playerCameraReference.position).eulerAngles;
			float num = eulerAngles.x - eulerAngles2.x;
			float num2 = eulerAngles2.y - eulerAngles.y;
			shakeValues = new ShakeEffectValues(null, null, null, 1f, num, num2);
			return false;
		}

		private readonly Vector3 _point;
	}
}
