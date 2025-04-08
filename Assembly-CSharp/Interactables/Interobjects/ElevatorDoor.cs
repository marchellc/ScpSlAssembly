using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using UnityEngine;

namespace Interactables.Interobjects
{
	public class ElevatorDoor : BasicDoor, INonInteractableDoor, IServerInteractable, IInteractable
	{
		public static event Action<ElevatorGroup> OnLocksChanged;

		public Vector3 TargetPosition
		{
			get
			{
				return base.transform.TransformPoint(this._targetPosition);
			}
		}

		public Vector3 TopPosition
		{
			get
			{
				return base.transform.TransformPoint(this._topPosition);
			}
		}

		public Vector3 BottomPosition
		{
			get
			{
				return base.transform.TransformPoint(this._bottomPosition);
			}
		}

		public ElevatorGroup Group
		{
			get
			{
				return this._group;
			}
		}

		public bool IgnoreLockdowns
		{
			get
			{
				return true;
			}
		}

		public bool IgnoreRemoteAdmin
		{
			get
			{
				return true;
			}
		}

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
				if (y < doorsForGroup[i].TargetPosition.y)
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
			if (!this.Chamber.IsReadyForUserInput)
			{
				return;
			}
			if (this.ActiveLocks != 0)
			{
				return;
			}
			this.Chamber.ServerInteract(ply, colliderId);
		}

		protected override void LockChanged(ushort prevValue)
		{
			base.LockChanged(prevValue);
			Action<ElevatorGroup> onLocksChanged = ElevatorDoor.OnLocksChanged;
			if (onLocksChanged == null)
			{
				return;
			}
			onLocksChanged(this._group);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			ElevatorDoor.GetDoorsForGroup(this._group).Remove(this);
		}

		public static List<ElevatorDoor> GetDoorsForGroup(ElevatorGroup group)
		{
			return ElevatorDoor.AllElevatorDoors.GetOrAdd(group, () => new List<ElevatorDoor>());
		}

		public override bool Weaved()
		{
			return true;
		}

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
	}
}
