using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace Interactables.Interobjects;

public class ElevatorDoor : BasicDoor, INonInteractableDoor, IServerInteractable, IInteractable
{
	private static readonly Dictionary<ElevatorGroup, List<ElevatorDoor>> AllElevatorDoors = new Dictionary<ElevatorGroup, List<ElevatorDoor>>();

	[SerializeField]
	private ElevatorGroup _group;

	[SerializeField]
	private Vector3 _targetPosition;

	[SerializeField]
	private Vector3 _topPosition;

	[SerializeField]
	private Vector3 _bottomPosition;

	[NonSerialized]
	public ElevatorChamber Chamber;

	public Vector3 TargetPosition => base.transform.TransformPoint(this._targetPosition);

	public Vector3 TopPosition => base.transform.TransformPoint(this._topPosition);

	public Vector3 BottomPosition => base.transform.TransformPoint(this._bottomPosition);

	public ElevatorGroup Group => this._group;

	public bool IgnoreLockdowns => true;

	public bool IgnoreRemoteAdmin => true;

	public static event Action<ElevatorGroup> OnLocksChanged;

	public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
	{
		return false;
	}

	protected override void Awake()
	{
		base.Awake();
		List<ElevatorDoor> doorsForGroup = ElevatorDoor.GetDoorsForGroup(this._group);
		bool flag = false;
		float y = this.TargetPosition.y;
		for (int i = 0; i < doorsForGroup.Count; i++)
		{
			if (!(y >= doorsForGroup[i].TargetPosition.y))
			{
				doorsForGroup.Insert(i, this);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			doorsForGroup.Add(this);
		}
	}

	public new void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (this.Chamber.IsReadyForUserInput && base.ActiveLocks == 0)
		{
			this.Chamber.ServerInteract(ply, colliderId);
		}
	}

	protected override void LockChanged(ushort prevValue)
	{
		base.LockChanged(prevValue);
		ElevatorDoor.OnLocksChanged?.Invoke(this._group);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		ElevatorDoor.GetDoorsForGroup(this._group).Remove(this);
	}

	public static List<ElevatorDoor> GetDoorsForGroup(ElevatorGroup group)
	{
		return ElevatorDoor.AllElevatorDoors.GetOrAddNew(group);
	}

	public override bool Weaved()
	{
		return true;
	}
}
