using UnityEngine;

namespace Waits;

public class AnimatorUntilWait : UntilWait
{
	public Animator animator;

	protected override bool Predicate()
	{
		return this.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f;
	}
}
