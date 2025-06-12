using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;

namespace CustomPlayerEffects;

public class Hemorrhage : TickingEffectBase
{
	public float damagePerTick = 1f;

	private bool _isSprinting;

	protected override void OnTick()
	{
		if (NetworkServer.active && this._isSprinting)
		{
			float damage = this.damagePerTick * RainbowTaste.CurrentMultiplier(base.Hub);
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Bleeding));
		}
	}

	protected override void OnEffectUpdate()
	{
		base.OnEffectUpdate();
		if (base.Hub.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			this._isSprinting = fpcRole.FpcModule.CurrentMovementState == PlayerMovementState.Sprinting;
		}
	}
}
