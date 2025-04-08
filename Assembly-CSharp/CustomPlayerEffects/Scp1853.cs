using System;
using System.Collections.Generic;
using CustomPlayerEffects.Danger;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Searching;
using Mirror;
using PlayerRoles.Spectating;
using RemoteAdmin.Interfaces;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Scp1853 : TickingEffectBase, ISpectatorDataPlayerEffect, IHealableEffect, IConflictableEffect, IWeaponModifierPlayerEffect, ISearchTimeModifier, ICustomRADisplay, IUsableItemModifierEffect, IHeavyItemPenaltyImmunity, ISoundtrackMutingEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public override byte MaxIntensity
		{
			get
			{
				return 20;
			}
		}

		public bool ParamsActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float ItemUsageSpeedMultiplier { get; private set; }

		public string DisplayName
		{
			get
			{
				return "SCP-1853";
			}
		}

		public bool CanBeDisplayed
		{
			get
			{
				return true;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return false;
			}
		}

		public bool MuteSoundtrack
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float CurrentDanger
		{
			get
			{
				return (float)base.Intensity * 0.25f;
			}
		}

		public float StatMultiplier
		{
			get
			{
				return 1f + (float)this.OffsetedDanger * 0.5f;
			}
		}

		private int OffsetedDanger
		{
			get
			{
				return Mathf.Max(0, Mathf.FloorToInt(this.CurrentDanger - 1f));
			}
		}

		protected override void IntensityChanged(byte prevState, byte newState)
		{
			float statMultiplier = this.StatMultiplier;
			float num = Mathf.Floor((float)prevState * 0.25f);
			float num2 = Mathf.Floor((float)newState * 0.25f);
			foreach (Scp1853.SerializedStat serializedStat in Scp1853.BaseWeaponModifiers)
			{
				float num3 = serializedStat.BoostPercentage * statMultiplier;
				this._processedParams[serializedStat.Parameter] = this.CalculateStat(num3, serializedStat.MaxBoost, serializedStat.IsAdditive);
			}
			this._searchSpeedMultiplier = 1.5f;
			this.ItemUsageSpeedMultiplier = this.CalculateStat(0.2f * statMultiplier, 0.4f, true);
			if (!base.IsLocalPlayer && !base.Hub.IsLocallySpectated())
			{
				return;
			}
			this.UpdateHeartbeat();
			if (num == num2 || num == 0f || num2 == 0f)
			{
				return;
			}
			bool flag = num2 > num;
			this._dangerChangedSource.PlayOneShot(flag ? this._dangerIncreasedClip : this._dangerDecreasedClip, 1f);
		}

		public override void OnStopSpectating()
		{
			base.OnStopSpectating();
			this.UpdateHeartbeat();
		}

		protected override void OnTick()
		{
		}

		protected override void Enabled()
		{
			base.Enabled();
			this.UpdateHeartbeat();
			if (!NetworkServer.active)
			{
				return;
			}
			DangerStackBase[] dangers = this.Dangers;
			for (int i = 0; i < dangers.Length; i++)
			{
				dangers[i].Initialize(base.Hub);
			}
		}

		protected override void Disabled()
		{
			base.Disabled();
			this.UpdateHeartbeat();
			if (!NetworkServer.active)
			{
				return;
			}
			DangerStackBase[] dangers = this.Dangers;
			for (int i = 0; i < dangers.Length; i++)
			{
				dangers[i].Dispose();
			}
		}

		protected override void Update()
		{
			base.Update();
			if (!base.IsEnabled || !NetworkServer.active)
			{
				return;
			}
			float num = 1f;
			foreach (DangerStackBase dangerStackBase in this.Dangers)
			{
				if (dangerStackBase.IsActive)
				{
					num += dangerStackBase.DangerValue;
				}
			}
			num = Mathf.Clamp(num, 1f, 5f);
			byte b = (byte)(num / 0.25f);
			if (b == base.Intensity)
			{
				return;
			}
			base.Intensity = b;
		}

		private void ExecuteVignettePulse()
		{
		}

		private void UpdateHeartbeat()
		{
		}

		private float CalculateStat(float boostAmount, float max = 0f, bool isAdditive = true)
		{
			if (max > 0f)
			{
				boostAmount = Mathf.Clamp(boostAmount, 0f, max);
			}
			return 1f + (isAdditive ? boostAmount : (-boostAmount));
		}

		public bool IsHealable(ItemType it)
		{
			return it == ItemType.SCP500;
		}

		public bool GetSpectatorText(out string s)
		{
			s = string.Format("SCP-1853 ({0} danger)", this.CurrentDanger);
			return true;
		}

		public bool TryGetSpeed(ItemType item, out float speed)
		{
			speed = this.ItemUsageSpeedMultiplier;
			return base.IsEnabled && Scp1853.AffectedItems.Contains(item);
		}

		public float ProcessSearchTime(float val)
		{
			return val / Mathf.Max(this._searchSpeedMultiplier, 1f);
		}

		public bool TryGetWeaponParam(AttachmentParam param, out float val)
		{
			return this._processedParams.TryGetValue(param, out val);
		}

		public bool CheckConflicts(StatusEffectBase other)
		{
			if (!(other is CokeBase))
			{
				return false;
			}
			Poisoned poisoned;
			if (!base.Hub.playerEffectsController.TryGetEffect<Poisoned>(out poisoned))
			{
				return false;
			}
			if (!poisoned.IsEnabled)
			{
				poisoned.ForceIntensity(1);
			}
			return true;
		}

		public const float DangerPerIntensity = 0.25f;

		private const float MinimumDanger = 1f;

		private const float MaximumDanger = 5f;

		private const float SearchSpeed = 1.5f;

		private const float EquipAndUse = 0.2f;

		private const float ItemUsageMaxMultiplier = 0.4f;

		private const float NoMaxBoost = 0f;

		private const float StatMultiplierPerDanger = 0.5f;

		private static readonly Scp1853.SerializedStat[] BaseWeaponModifiers = new Scp1853.SerializedStat[]
		{
			new Scp1853.SerializedStat(AttachmentParam.AdsSpeedMultiplier, 0.2f, 0.4f, true),
			new Scp1853.SerializedStat(AttachmentParam.OverallRecoilMultiplier, 0.2f, 0f, false),
			new Scp1853.SerializedStat(AttachmentParam.AdsInaccuracyMultiplier, 0.2f, 0f, false),
			new Scp1853.SerializedStat(AttachmentParam.HipInaccuracyMultiplier, 0.2f, 0f, false),
			new Scp1853.SerializedStat(AttachmentParam.ReloadSpeedMultiplier, 0.35f, 0f, true),
			new Scp1853.SerializedStat(AttachmentParam.DrawSpeedMultiplier, 0.2f, 0f, true)
		};

		private static readonly ItemType[] AffectedItems = new ItemType[]
		{
			ItemType.SCP018,
			ItemType.SCP500,
			ItemType.SCP330,
			ItemType.GrenadeFlash,
			ItemType.GrenadeHE,
			ItemType.Medkit,
			ItemType.Painkillers,
			ItemType.Adrenaline
		};

		public DangerStackBase[] Dangers = new DangerStackBase[]
		{
			new WarheadDanger(),
			new CardiacArrestDanger(),
			new RageTargetDanger(),
			new CorrodingDanger(),
			new PlayerDamagedDanger(),
			new ScpEncounterDanger(),
			new ZombieEncounterDanger(),
			new ArmedEnemyDanger()
		};

		[SerializeField]
		private AudioSource _dangerChangedSource;

		[SerializeField]
		private AudioSource _heartbeatSource;

		[SerializeField]
		private AudioClip _dangerIncreasedClip;

		[SerializeField]
		private AudioClip _dangerDecreasedClip;

		private readonly Dictionary<AttachmentParam, float> _processedParams = new Dictionary<AttachmentParam, float>();

		private float _searchSpeedMultiplier;

		[Serializable]
		private struct SerializedStat
		{
			public SerializedStat(AttachmentParam param, float boostPercentage, float maxBoost = 0f, bool isAdditive = true)
			{
				this.Parameter = param;
				this.BoostPercentage = boostPercentage;
				this.MaxBoost = maxBoost;
				this.IsAdditive = isAdditive;
			}

			public AttachmentParam Parameter;

			public float BoostPercentage;

			public float MaxBoost;

			public bool IsAdditive;
		}
	}
}
