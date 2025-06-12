using Interactables.Interobjects;
using UnityEngine;

namespace Elevators;

public abstract class ElevatorFollowerBase : MonoBehaviour
{
	public Vector3 LastPosition;

	public ElevatorChamber TrackedChamber { get; private set; }

	public bool InElevator { get; private set; }

	protected virtual void Awake()
	{
		ElevatorChamber.OnElevatorMoved += OnElevatorMoved;
	}

	protected virtual void OnDestroy()
	{
		ElevatorChamber.OnElevatorMoved -= OnElevatorMoved;
	}

	protected virtual void LateUpdate()
	{
	}

	protected virtual void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
	{
		bool flag = this.InElevator && chamber == this.TrackedChamber;
		if (!elevatorBounds.Contains(this.LastPosition))
		{
			if (flag)
			{
				this.InElevator = false;
				base.transform.position -= deltaPos;
				base.transform.SetParent(null);
			}
		}
		else if (!flag)
		{
			base.transform.SetParent(chamber.transform);
			base.transform.position += deltaPos;
			this.TrackedChamber = chamber;
			this.InElevator = true;
		}
	}
}
