using AudioPooling;
using Footprinting;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects;

public class CardiacArrest : ParentEffectBase<SubEffectBase>, IHealableEffect, IStaminaModifier
{
	private const float SprintStaminaUsage = 3f;

	private const float DamagePerTick = 8f;

	private Footprint _attacker;

	[SerializeField]
	private AudioClip _dyingSoundEffect;

	[Tooltip("Used to track intervals/timers/etc without every effect needing to redefine a unique float.")]
	public float TimeBetweenTicks;

	private float _timeTillTick;

	private AudioPoolSession _dyingSoundSession;

	public bool StaminaModifierActive => base.IsEnabled;

	public float StaminaUsageMultiplier => 3f;

	public float StaminaRegenMultiplier => 1f;

	public bool SprintingDisabled => false;

	public override bool AllowEnabling => !SpawnProtected.CheckPlayer(base.Hub);

	protected override void Enabled()
	{
		base.Enabled();
		if (base.Hub.isLocalPlayer || base.Hub.IsLocallySpectated())
		{
			this._dyingSoundSession = new AudioPoolSession(AudioSourcePoolManager.Play2D(this._dyingSoundEffect));
		}
		if (NetworkServer.active)
		{
			this._timeTillTick = 0f;
		}
	}

	protected override void Disabled()
	{
		base.Disabled();
		this._attacker = default(Footprint);
	}

	public void SetAttacker(ReferenceHub ply)
	{
		this._attacker = new Footprint(ply);
	}

	public bool IsHealable(ItemType it)
	{
		if (it != ItemType.SCP500)
		{
			return it == ItemType.Adrenaline;
		}
		return true;
	}

	protected override void OnEffectUpdate()
	{
		if (NetworkServer.active)
		{
			this.ServerUpdate();
		}
		this.UpdateSubEffects();
	}

	public override void OnStopSpectating()
	{
		base.OnStopSpectating();
		if (this._dyingSoundSession.SameSession)
		{
			this._dyingSoundSession.Source.Stop();
		}
	}

	private void ServerUpdate()
	{
		this._timeTillTick -= Time.deltaTime;
		if (!(this._timeTillTick > 0f))
		{
			this._timeTillTick += this.TimeBetweenTicks;
			base.Hub.playerStats.DealDamage(new Scp049DamageHandler(this._attacker, 8f, Scp049DamageHandler.AttackType.CardiacArrest));
		}
	}
}
