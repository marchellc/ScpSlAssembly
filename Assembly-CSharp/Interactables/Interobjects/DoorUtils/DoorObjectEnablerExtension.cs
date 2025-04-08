using System;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	public class DoorObjectEnablerExtension : DoorVariantExtension
	{
		private void Start()
		{
			GameObject[] objects = this.Objects;
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].SetActive(false);
			}
		}

		private void Update()
		{
			bool targetState = this.TargetDoor.TargetState;
			bool isMoving = this.TargetDoor.IsMoving;
			PryableDoor pryableDoor = this.TargetDoor as PryableDoor;
			bool flag = pryableDoor != null && pryableDoor.IsBeingPried;
			bool flag2 = (this.EnableOnPry || !flag) && ((this.EnableOnOpen && targetState) || (this.EnableOnClose && !targetState) || (this.EnableOnMoving && isMoving) || (this.EnableOnPry && flag));
			if (flag2 == this._previousState)
			{
				return;
			}
			this._previousState = flag2;
			GameObject[] objects = this.Objects;
			for (int i = 0; i < objects.Length; i++)
			{
				objects[i].SetActive(flag2);
			}
		}

		public GameObject[] Objects;

		public bool EnableOnOpen = true;

		public bool EnableOnClose = true;

		public bool EnableOnMoving = true;

		public bool EnableOnPry = true;

		private bool _previousState;
	}
}
