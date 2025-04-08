using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class AntiScp207 : CokeBase<AntiScp207Stack>, ISpectatorDataPlayerEffect, ICustomRADisplay, IDamageModifierEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Mixed;
			}
		}

		public override Dictionary<PlayerMovementState, float> StateMultipliers { get; }

		public string DisplayName
		{
			get
			{
				return "SCP-207?";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return false;
			}
		}

		public override float MovementSpeedMultiplier
		{
			get
			{
				if (!Vitality.CheckPlayer(base.Hub))
				{
					return base.CurrentStack.SpeedMultiplier;
				}
				return AntiScp207.VitalitySpeedMultipler;
			}
		}

		public bool DamageModifierActive
		{
			get
			{
				return this._isDamageModifierEnabled && (base.IsEnabled || this.IsTeslaImmunityActive);
			}
		}

		private bool IsTeslaImmunityActive
		{
			get
			{
				return this._lastSaveTime + AntiScp207.TeslaImmunityTime >= Time.timeSinceLevelLoad;
			}
		}

		private bool IsImmunityActive
		{
			get
			{
				return this._lastSaveTime + AntiScp207.DamageImmunityTime >= Time.timeSinceLevelLoad;
			}
		}

		private float CurrentHealing
		{
			get
			{
				return base.CurrentStack.HealAmount * base.GetMovementStateMultiplier();
			}
		}

		public override bool CheckConflicts(StatusEffectBase other)
		{
			return this._isDamageModifierEnabled = !base.CheckConflicts(other);
		}

		public bool GetSpectatorText(out string s)
		{
			s = ((base.Intensity > 1) ? string.Format("SCP-207? (x{0})", base.Intensity) : "SCP-207?");
			return true;
		}

		public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
		{
			UniversalDamageHandler universalDamageHandler = handler as UniversalDamageHandler;
			if (this.IsImmunityActive)
			{
				return 0f;
			}
			if (this.IsTeslaImmunityActive && universalDamageHandler != null && universalDamageHandler.TranslationId == DeathTranslations.Tesla.Id)
			{
				return 0f;
			}
			if (!base.IsEnabled)
			{
				return 1f;
			}
			if (universalDamageHandler != null && (universalDamageHandler.TranslationId == DeathTranslations.Scp207.Id || universalDamageHandler.TranslationId == DeathTranslations.PocketDecay.Id))
			{
				return 1f;
			}
			float curValue = base.Hub.playerStats.GetModule<HealthStat>().CurValue;
			float curValue2 = base.Hub.playerStats.GetModule<AhpStat>().CurValue;
			float num = curValue + curValue2;
			if (curValue > baseDamage || num > baseDamage)
			{
				return 1f;
			}
			this.DisableEffect();
			NetworkServer.SendToReady<AntiScp207.BreakMessage>(new AntiScp207.BreakMessage
			{
				SoundPos = base.Hub.transform.position
			}, 0);
			this._lastSaveTime = Time.timeSinceLevelLoad;
			return (num - AntiScp207.DeathSaveHealth) / baseDamage;
		}

		protected override void IntensityChanged(byte prevState, byte newState)
		{
			base.IntensityChanged(prevState, newState);
			if (!base.IsLocalPlayer)
			{
				return;
			}
			this._ambienceSource.Stop();
			if (newState > 0)
			{
				this._ambienceSource.clip = this._ambienceSounds[Mathf.Min((int)newState, this._ambienceSounds.Length) - 1];
				this._ambienceSource.Play();
			}
		}

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			float currentHealing = this.CurrentHealing;
			if (!this._healthStat.FullyHealed)
			{
				if (this._ahpProcess != null)
				{
					this._ahpProcess.DecayRate = 0f;
				}
				this._healthStat.ServerHeal(currentHealing);
				return;
			}
			if (this._ahpProcess == null)
			{
				this._ahpProcess = this._ahpStat.ServerAddProcess(0f, this._ahpStat.MaxValue, -currentHealing, 0.7f, 0f, true);
			}
			this._ahpProcess.DecayRate = -currentHealing;
		}

		protected override void Enabled()
		{
			base.Enabled();
			this._isDamageModifierEnabled = true;
			this._ahpProcess = null;
			if (!base.Hub.playerStats.TryGetModule<HealthStat>(out this._healthStat))
			{
				throw new NullReferenceException();
			}
			if (!base.Hub.playerStats.TryGetModule<AhpStat>(out this._ahpStat))
			{
				throw new NullReferenceException();
			}
		}

		protected override void Disabled()
		{
			base.Disabled();
			if (this._ahpProcess == null)
			{
				return;
			}
			this._ahpProcess.DecayRate = 1f;
		}

		private static void OnBreak(AntiScp207.BreakMessage msg)
		{
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<AntiScp207.BreakMessage>(new Action<AntiScp207.BreakMessage>(AntiScp207.OnBreak), true);
			};
		}

		public AntiScp207()
		{
			Dictionary<PlayerMovementState, float> dictionary = new Dictionary<PlayerMovementState, float>();
			dictionary[PlayerMovementState.Crouching] = 1f;
			dictionary[PlayerMovementState.Sneaking] = 0.7f;
			dictionary[PlayerMovementState.Walking] = 0.6f;
			dictionary[PlayerMovementState.Sprinting] = 0.1f;
			this.StateMultipliers = dictionary;
			base..ctor();
		}

		private const float AhpDecayValue = 1f;

		public static readonly float DeathSaveHealth = 1f;

		public static readonly float VitalitySpeedMultipler = 1f;

		public static readonly float HumanAHPEfficacy = 1f;

		public static readonly float TeslaImmunityTime = 2f;

		public static readonly float BreakSoundDistance = 25f;

		public static readonly float BreakVolumeModifier = 0.45f;

		public static readonly float DamageImmunityTime = 1.5f;

		[SerializeField]
		private AudioClip[] _ambienceSounds;

		[SerializeField]
		private AudioSource _ambienceSource;

		[SerializeField]
		private AudioClip _effectBreakSound;

		private float _lastSaveTime;

		private bool _isDamageModifierEnabled;

		private AhpStat.AhpProcess _ahpProcess;

		private HealthStat _healthStat;

		private AhpStat _ahpStat;

		private struct BreakMessage : NetworkMessage
		{
			public Vector3 SoundPos;
		}
	}
}
