using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace LightContainmentZoneDecontamination;

[Obsolete("Replaced by the new door system", true)]
public class DecontaminationEvacuationDoor : MonoBehaviour
{
	public static List<DecontaminationEvacuationDoor> Instances = new List<DecontaminationEvacuationDoor>();

	public bool ShouldBeOpened = true;

	public bool ShouldBeClosed = true;

	[SerializeField]
	private DoorVariant _door;

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
		if (this.ShouldBeOpened)
		{
			this._door.NetworkTargetState = true;
			this._door.ServerChangeLock(DoorLockReason.DecontEvacuate, newState: true);
		}
	}

	public void Close()
	{
		if (this.ShouldBeClosed)
		{
			this._door.NetworkTargetState = false;
			this._door.ServerChangeLock(DoorLockReason.DecontEvacuate, newState: false);
			this._door.ServerChangeLock(DoorLockReason.DecontLockdown, newState: true);
		}
	}
}
