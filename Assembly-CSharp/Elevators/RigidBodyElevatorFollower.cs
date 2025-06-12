using UnityEngine;

namespace Elevators;

public class RigidBodyElevatorFollower : ElevatorFollowerBase
{
	public Rigidbody Rigidbody;

	private bool _unlinked;

	protected override void Awake()
	{
		base.Awake();
		base.LastPosition = this.Rigidbody.position;
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (!this._unlinked && !this.Rigidbody.IsSleeping())
		{
			base.LastPosition = this.Rigidbody.position;
		}
	}

	private void Reset()
	{
		this.Rigidbody = base.GetComponent<Rigidbody>();
	}

	public void Unlink()
	{
		this._unlinked = true;
	}
}
