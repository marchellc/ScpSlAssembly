using UnityEngine;

namespace Elevators;

public class TransformElevatorFollower : ElevatorFollowerBase
{
	private Transform _cachedTransform;

	protected override void Awake()
	{
		base.Awake();
		this._cachedTransform = base.gameObject.transform;
		base.LastPosition = this._cachedTransform.position;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		base.LastPosition = this._cachedTransform.position;
	}
}
