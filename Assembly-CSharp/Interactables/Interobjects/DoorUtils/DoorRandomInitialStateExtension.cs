using System;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public class DoorRandomInitialStateExtension : DoorVariantExtension
	{
		private void Start()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this.TargetDoor.NetworkTargetState = global::UnityEngine.Random.value < this.OpenChance;
		}

		[Range(0f, 1f)]
		public float OpenChance = 0.5f;
	}
}
