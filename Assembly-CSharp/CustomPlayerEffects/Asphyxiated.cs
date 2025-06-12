using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class Asphyxiated : TickingEffectBase, IStaminaModifier
{
	public float staminaDrainPerTick = 5f;

	public float healthDrainPerTick = 2f;

	private StaminaStat _stamina;

	public bool StaminaModifierActive => base.IsEnabled;

	public float StaminaUsageMultiplier => 1f;

	public float StaminaRegenMultiplier => 0f;

	public bool SprintingDisabled => false;

	protected override void Enabled()
	{
		base.Enabled();
		this._stamina = base.Hub.playerStats.GetModule<StaminaStat>();
	}

	protected override void OnTick()
	{
		if (NetworkServer.active)
		{
			this._stamina.CurValue = Mathf.Clamp01(this._stamina.CurValue - this.staminaDrainPerTick * 0.01f);
			if (this._stamina.CurValue <= 0f)
			{
				float damage = this.healthDrainPerTick * RainbowTaste.CurrentMultiplier(base.Hub);
				base.Hub.playerStats.DealDamage(new UniversalDamageHandler(damage, DeathTranslations.Asphyxiated));
			}
		}
	}
}
