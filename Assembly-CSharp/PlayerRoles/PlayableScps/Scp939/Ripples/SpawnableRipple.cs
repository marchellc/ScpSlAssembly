using System;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples
{
	public class SpawnableRipple : MonoBehaviour
	{
		public static event Action<SpawnableRipple> OnSpawned;

		public float Range { get; private set; }

		private void OnEnabled()
		{
			Action<SpawnableRipple> onSpawned = SpawnableRipple.OnSpawned;
			if (onSpawned == null)
			{
				return;
			}
			onSpawned(this);
		}
	}
}
