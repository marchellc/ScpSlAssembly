using System;
using Interactables.Interobjects;
using UnityEngine;

namespace Elevators
{
	public abstract class ElevatorFollowerBase : MonoBehaviour
	{
		public ElevatorChamber TrackedChamber { get; private set; }

		public bool InElevator { get; private set; }

		protected virtual void Awake()
		{
			ElevatorChamber.OnElevatorMoved += this.OnElevatorMoved;
		}

		protected virtual void OnDestroy()
		{
			ElevatorChamber.OnElevatorMoved -= this.OnElevatorMoved;
		}

		protected virtual void LateUpdate()
		{
		}

		protected virtual void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
		{
			bool flag = this.InElevator && chamber == this.TrackedChamber;
			if (!elevatorBounds.Contains(this.LastPosition))
			{
				if (!flag)
				{
					return;
				}
				this.InElevator = false;
				base.transform.position -= deltaPos;
				base.transform.SetParent(null);
				return;
			}
			else
			{
				if (flag)
				{
					return;
				}
				base.transform.SetParent(chamber.transform);
				base.transform.position += deltaPos;
				this.TrackedChamber = chamber;
				this.InElevator = true;
				return;
			}
		}

		public Vector3 LastPosition;
	}
}
