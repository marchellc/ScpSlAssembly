using System;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class SeveredEyes : TickingEffectBase
	{
		public override bool AllowEnabling
		{
			get
			{
				return true;
			}
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this._damagePerTick, DeathTranslations.Scp1344, null));
		}

		[SerializeField]
		private float _damagePerTick;
	}
}
