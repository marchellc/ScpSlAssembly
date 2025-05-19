using System;
using Mirror;

namespace Achievements;

public abstract class AchievementHandlerBase
{
	internal virtual void OnInitialize()
	{
	}

	internal virtual void OnRoundStarted()
	{
	}

	public static void ClientAchieve(AchievementName targetAchievement)
	{
		if (AchievementManager.Achievements.TryGetValue(targetAchievement, out var value))
		{
			value.Achieve();
		}
	}

	public static void ServerAchieve(NetworkConnection conn, AchievementName targetAchievement)
	{
		if (!NetworkServer.active)
		{
			throw new InvalidOperationException("Method ServerAchieve can only be executed on the server.");
		}
		if (!conn.identity.isLocalPlayer)
		{
			conn.Send(new AchievementManager.AchievementMessage
			{
				AchievementId = (byte)targetAchievement
			});
		}
	}
}
