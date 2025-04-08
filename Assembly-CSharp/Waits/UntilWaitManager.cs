using System;
using System.Collections.Generic;
using MEC;

namespace Waits
{
	public abstract class UntilWaitManager : WaitManager
	{
		protected override void Awake()
		{
			base.Awake();
			this.allocatedKeepRunning = new Func<bool>(this.KeepRunning);
		}

		protected abstract bool KeepRunning();

		public override IEnumerator<float> _Run()
		{
			base.StartAll();
			yield return float.NegativeInfinity;
			yield return Timing.WaitUntilFalse(this.allocatedKeepRunning);
			yield break;
		}

		protected Func<bool> allocatedKeepRunning;
	}
}
