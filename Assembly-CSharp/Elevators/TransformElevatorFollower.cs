using System;
using UnityEngine;

namespace Elevators
{
	public class TransformElevatorFollower : ElevatorFollowerBase
	{
		protected override void Awake()
		{
			base.Awake();
			this._cachedTransform = base.gameObject.transform;
			this.LastPosition = this._cachedTransform.position;
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			this.LastPosition = this._cachedTransform.position;
		}

		private Transform _cachedTransform;
	}
}
