using System;
using PlayerRoles.Voice;

namespace Achievements.Handlers
{
	public class IntercomHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			Intercom.OnServerBeginUsage += this.OnBeginUsage;
		}

		private void OnBeginUsage(ReferenceHub hub)
		{
			if (hub == null)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(hub.connectionToClient, AchievementName.IsThisThingOn);
		}
	}
}
