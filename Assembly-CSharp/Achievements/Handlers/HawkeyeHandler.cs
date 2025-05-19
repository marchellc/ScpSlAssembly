using CustomPlayerEffects;
using PlayerRoles;

namespace Achievements.Handlers;

public class HawkeyeHandler : AchievementHandlerBase
{
	internal override void OnInitialize()
	{
		Scp1344.OnPlayerSeen += OnPlayerSeen;
	}

	private void OnPlayerSeen(ReferenceHub owner, ReferenceHub seenPlayer)
	{
		if (ReferenceHub.TryGetLocalHub(out var hub) && hub.IsAlive() && seenPlayer.playerEffectsController.TryGetEffect<Invisible>(out var playerEffect) && playerEffect.IsEnabled && HitboxIdentity.IsEnemy(owner, seenPlayer))
		{
			AchievementHandlerBase.ClientAchieve(AchievementName.Hawkeye);
		}
	}
}
