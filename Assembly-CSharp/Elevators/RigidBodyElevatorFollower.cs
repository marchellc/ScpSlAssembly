using System;
using UnityEngine;

namespace Elevators
{
	public class RigidBodyElevatorFollower : ElevatorFollowerBase
	{
		protected override void Awake()
		{
			base.Awake();
			this.LastPosition = this.Rigidbody.position;
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			if (this._unlinked || this.Rigidbody.IsSleeping())
			{
				return;
			}
			this.LastPosition = this.Rigidbody.position;
		}

		private void Reset()
		{
			this.Rigidbody = base.GetComponent<Rigidbody>();
		}

		public void Unlink()
		{
			this._unlinked = true;
		}

		public Rigidbody Rigidbody;

		private bool _unlinked;
	}
}
