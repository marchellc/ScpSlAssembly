using System;
using UnityEngine;

namespace Waits
{
	public class VelocityUntilWait : UntilWait
	{
		protected override void Awake()
		{
			base.Awake();
			this.sqrThreshold = this.threshold * this.threshold;
		}

		protected override bool Predicate()
		{
			return this.rigidbody.velocity.sqrMagnitude < this.sqrThreshold;
		}

		[NonSerialized]
		private float sqrThreshold;

		public Rigidbody rigidbody;

		public float threshold = 0.05f;
	}
}
