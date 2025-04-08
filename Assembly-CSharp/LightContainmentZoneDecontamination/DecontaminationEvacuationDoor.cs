using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace LightContainmentZoneDecontamination
{
	[Obsolete("Replaced by the new door system", true)]
	public class DecontaminationEvacuationDoor : MonoBehaviour
	{
		private void Awake()
		{
			DecontaminationEvacuationDoor.Instances.Add(this);
		}

		private void OnDestroy()
		{
			DecontaminationEvacuationDoor.Instances.Remove(this);
		}

		public void Open()
		{
			if (!this.ShouldBeOpened)
			{
				return;
			}
			this._door.NetworkTargetState = true;
			this._door.ServerChangeLock(DoorLockReason.DecontEvacuate, true);
		}

		public void Close()
		{
			if (!this.ShouldBeClosed)
			{
				return;
			}
			this._door.NetworkTargetState = false;
			this._door.ServerChangeLock(DoorLockReason.DecontEvacuate, false);
			this._door.ServerChangeLock(DoorLockReason.DecontLockdown, true);
		}

		public static List<DecontaminationEvacuationDoor> Instances = new List<DecontaminationEvacuationDoor>();

		public bool ShouldBeOpened = true;

		public bool ShouldBeClosed = true;

		[SerializeField]
		private DoorVariant _door;
	}
}
