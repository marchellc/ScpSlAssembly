using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class SeveredEyes : TickingEffectBase
{
	[SerializeField]
	private float _damagePerTick;

	public override bool AllowEnabling => true;

	protected override void OnTick()
	{
		if (NetworkServer.active)
		{
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this._damagePerTick, DeathTranslations.Scp1344));
		}
	}
}
