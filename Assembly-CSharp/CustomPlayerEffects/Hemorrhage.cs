using System;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;

namespace CustomPlayerEffects
{
	public class Hemorrhage : TickingEffectBase
	{
		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (this._isSprinting)
			{
				float num = this.damagePerTick * RainbowTaste.CurrentMultiplier(base.Hub);
				base.Hub.playerStats.DealDamage(new UniversalDamageHandler(num, DeathTranslations.Bleeding, null));
			}
		}

		protected override void OnEffectUpdate()
		{
			base.OnEffectUpdate();
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			this._isSprinting = fpcRole.FpcModule.CurrentMovementState == PlayerMovementState.Sprinting;
		}

		public float damagePerTick = 1f;

		private bool _isSprinting;
	}
}
