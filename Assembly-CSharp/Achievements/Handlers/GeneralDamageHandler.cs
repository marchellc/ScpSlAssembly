using System;
using Mirror;
using PlayerRoles;
using PlayerStatsSystem;

namespace Achievements.Handlers
{
	public class GeneralDamageHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			PlayerStats.OnAnyPlayerDamaged += GeneralDamageHandler.AnyDamage;
		}

		private static void AnyDamage(ReferenceHub ply, DamageHandlerBase handler)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			HealthStat module = ply.playerStats.GetModule<HealthStat>();
			UniversalDamageHandler universalDamageHandler = handler as UniversalDamageHandler;
			if (universalDamageHandler != null && universalDamageHandler.TranslationId == DeathTranslations.Falldown.Id && module.CurValue > 0f)
			{
				float num = module.CurValue + universalDamageHandler.Damage;
				if (num > 0f && module.CurValue / num < 0.5f)
				{
					AchievementHandlerBase.ServerAchieve(ply.connectionToClient, AchievementName.WalkItOff);
				}
			}
			StandardDamageHandler standardDamageHandler = handler as StandardDamageHandler;
			if (standardDamageHandler == null)
			{
				return;
			}
			if (!ply.IsHuman())
			{
				return;
			}
			if (module.CurValue > 0f && module.CurValue - standardDamageHandler.AbsorbedAhpDamage <= 0f)
			{
				AchievementHandlerBase.ServerAchieve(ply.networkIdentity.connectionToClient, AchievementName.DidntEvenFeelThat);
			}
		}

		private const float WalkItOffThreshHold = 0.5f;
	}
}
