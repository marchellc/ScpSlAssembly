using System;
using CustomPlayerEffects;
using PlayerRoles;

namespace Achievements.Handlers
{
	public class HawkeyeHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			Scp1344.OnPlayerSeen += this.OnPlayerSeen;
		}

		private void OnPlayerSeen(ReferenceHub owner, ReferenceHub seenPlayer)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			if (!referenceHub.IsAlive())
			{
				return;
			}
			Invisible invisible;
			if (!seenPlayer.playerEffectsController.TryGetEffect<Invisible>(out invisible))
			{
				return;
			}
			if (!invisible.IsEnabled)
			{
				return;
			}
			if (!HitboxIdentity.IsEnemy(owner, seenPlayer))
			{
				return;
			}
			AchievementHandlerBase.ClientAchieve(AchievementName.Hawkeye);
		}
	}
}
