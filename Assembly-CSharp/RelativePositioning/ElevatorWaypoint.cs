using Interactables.Interobjects;
using UnityEngine;

namespace RelativePositioning;

public class ElevatorWaypoint : WaypointBase
{
	[SerializeField]
	private ElevatorChamber _elevator;

	private Transform _transform;

	private bool _transformSet;

	private Transform ElevatorTransform
	{
		get
		{
			if (!this._transformSet)
			{
				this._transform = this._elevator.transform;
				this._transformSet = true;
			}
			return this._transform;
		}
	}

	protected override void Start()
	{
		base.Start();
		base.SetId((byte)(this._elevator.AssignedGroup + 1));
	}

	protected override float SqrDistanceTo(Vector3 pos)
	{
		if (!this._elevator.WorldspaceBounds.Contains(pos))
		{
			return float.MaxValue;
		}
		return -1f;
	}

	public override Vector3 GetWorldspacePosition(Vector3 relPosition)
	{
		return this.ElevatorTransform.TransformPoint(relPosition);
	}

	public override Vector3 GetRelativePosition(Vector3 worldPoint)
	{
		return this.ElevatorTransform.InverseTransformPoint(worldPoint);
	}

	public override Quaternion GetWorldspaceRotation(Quaternion relRotation)
	{
		return this.ElevatorTransform.rotation * relRotation;
	}

	public override Quaternion GetRelativeRotation(Quaternion worldRot)
	{
		return Quaternion.Inverse(this.ElevatorTransform.rotation) * worldRot;
	}
}
