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
			if (!_transformSet)
			{
				_transform = _elevator.transform;
				_transformSet = true;
			}
			return _transform;
		}
	}

	protected override void Start()
	{
		base.Start();
		SetId((byte)(_elevator.AssignedGroup + 1));
	}

	protected override float SqrDistanceTo(Vector3 pos)
	{
		if (!_elevator.WorldspaceBounds.Contains(pos))
		{
			return float.MaxValue;
		}
		return -1f;
	}

	public override Vector3 GetWorldspacePosition(Vector3 relPosition)
	{
		return ElevatorTransform.TransformPoint(relPosition);
	}

	public override Vector3 GetRelativePosition(Vector3 worldPoint)
	{
		return ElevatorTransform.InverseTransformPoint(worldPoint);
	}

	public override Quaternion GetWorldspaceRotation(Quaternion relRotation)
	{
		return ElevatorTransform.rotation * relRotation;
	}

	public override Quaternion GetRelativeRotation(Quaternion worldRot)
	{
		return Quaternion.Inverse(ElevatorTransform.rotation) * worldRot;
	}
}
