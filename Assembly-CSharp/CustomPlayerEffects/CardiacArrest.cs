using System;
using AudioPooling;
using Footprinting;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class CardiacArrest : ParentEffectBase<SubEffectBase>, IHealableEffect, IStaminaModifier
	{
		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float StaminaUsageMultiplier
		{
			get
			{
				return 3f;
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return false;
			}
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			if (base.Hub.isLocalPlayer || base.Hub.IsLocallySpectated())
			{
				this._dyingSoundSession = new AudioPoolSession(AudioSourcePoolManager.Play2D(this._dyingSoundEffect, 1f, MixerChannel.DefaultSfx, 1f));
			}
			if (!NetworkServer.active)
			{
				return;
			}
			this._timeTillTick = 0f;
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
			return it == ItemType.SCP500 || it == ItemType.Adrenaline;
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
			if (!this._dyingSoundSession.SameSession)
			{
				return;
			}
			this._dyingSoundSession.Source.Stop();
		}

		private void ServerUpdate()
		{
			this._timeTillTick -= Time.deltaTime;
			if (this._timeTillTick > 0f)
			{
				return;
			}
			this._timeTillTick += this.TimeBetweenTicks;
			base.Hub.playerStats.DealDamage(new Scp049DamageHandler(this._attacker, 8f, Scp049DamageHandler.AttackType.CardiacArrest));
		}

		private const float SprintStaminaUsage = 3f;

		private const float DamagePerTick = 8f;

		private Footprint _attacker;

		[SerializeField]
		private AudioClip _dyingSoundEffect;

		[Tooltip("Used to track intervals/timers/etc without every effect needing to redefine a unique float.")]
		public float TimeBetweenTicks;

		private float _timeTillTick;

		private AudioPoolSession _dyingSoundSession;
	}
}
