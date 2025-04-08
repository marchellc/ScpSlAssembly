using System;
using UnityEngine;

namespace Waits
{
	public class AnimatorUntilWait : UntilWait
	{
		protected override bool Predicate()
		{
			return this.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
		}

		public Animator animator;
	}
}
