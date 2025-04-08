using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Waits
{
	public class AnimatorWait : Wait
	{
		public override IEnumerator<float> _Run()
		{
			yield return Timing.WaitUntilFalse(() => this.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f);
			yield break;
		}

		public Animator animator;
	}
}
