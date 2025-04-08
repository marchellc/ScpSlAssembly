using System;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public abstract class DoorVariantExtension : MonoBehaviour
	{
		private void OnValidate()
		{
			this.TargetDoor = base.GetComponent<DoorVariant>();
		}

		private void Awake()
		{
			if (!this.TargetDoor)
			{
				this.TargetDoor = base.GetComponent<DoorVariant>();
			}
		}

		public DoorVariant TargetDoor;
	}
}
