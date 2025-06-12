using System.Linq;
using MEC;

namespace Waits;

public class OrWaitManager : UntilWaitManager
{
	protected override bool KeepRunning()
	{
		return base.waitHandles.Any((CoroutineHandle x) => x.IsRunning);
	}
}
