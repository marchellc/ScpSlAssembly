using System;
using System.Collections.Generic;
using MEC;

namespace Waits
{
	public class TimeWait : Wait
	{
		public override IEnumerator<float> _Run()
		{
			yield return Timing.WaitForSeconds(this.duration);
			yield break;
		}

		public float duration = 10f;
	}
}
