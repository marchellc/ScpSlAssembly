using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class Exhausted : StatusEffectBase, IStaminaModifier
{
	private const float MaxStamina = 0.5f;

	private const float StaminaRegenSpeed = 0.5f;

	private StaminaStat _staminaCache;

	private bool _cacheSet;

	public bool StaminaModifierActive => base.IsEnabled;

	public float StaminaUsageMultiplier => 1f;

	public bool SprintingDisabled => false;

	public float StaminaRegenMultiplier
	{
		get
		{
			if (!(this.CurStamina < 0.5f))
			{
				return 0f;
			}
			return 0.5f;
		}
	}

	private float CurStamina
	{
		get
		{
			this.PrepCache();
			return this._staminaCache.CurValue;
		}
		set
		{
			this.PrepCache();
			this._staminaCache.CurValue = value;
		}
	}

	private void PrepCache()
	{
		if (!this._cacheSet)
		{
			this._cacheSet = true;
			this._staminaCache = base.Hub.playerStats.GetModule<StaminaStat>();
		}
	}

	protected override void Enabled()
	{
		base.Enabled();
		this.CurStamina = Mathf.Min(this.CurStamina, 0.5f);
	}
}
