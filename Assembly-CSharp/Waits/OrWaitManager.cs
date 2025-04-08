using System;
using System.Linq;
using MEC;

namespace Waits
{
	public class OrWaitManager : UntilWaitManager
	{
		protected override bool KeepRunning()
		{
			return this.waitHandles.Any((CoroutineHandle x) => x.IsRunning);
		}
	}
}
