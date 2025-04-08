using System;
using System.Linq;
using MEC;

namespace Waits
{
	public class AndWaitManager : UntilWaitManager
	{
		protected override bool KeepRunning()
		{
			return this.waitHandles.All((CoroutineHandle x) => x.IsRunning);
		}
	}
}
