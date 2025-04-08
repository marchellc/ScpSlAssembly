using System;
using System.Collections.Generic;
using MEC;

namespace Waits
{
	public abstract class UntilWait : Wait
	{
		protected virtual void Awake()
		{
			this.allocatedPredicate = new Func<bool>(this.Predicate);
		}

		protected abstract bool Predicate();

		public override IEnumerator<float> _Run()
		{
			yield return Timing.WaitUntilTrue(this.allocatedPredicate);
			yield break;
		}

		private Func<bool> allocatedPredicate;
	}
}
