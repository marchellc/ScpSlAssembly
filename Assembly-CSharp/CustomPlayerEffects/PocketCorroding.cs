using System;
using AudioPooling;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class PocketCorroding : TickingEffectBase, IFootstepEffect, IMovementSpeedModifier, IStaminaModifier
	{
		public override bool AllowEnabling
		{
			get
			{
				return true;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

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
				return 1f;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return 1f;
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
				return true;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		public RelativePosition CapturePosition { get; private set; }

		protected override void OnTick()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole != null && fpcRole.FpcModule.Position.y > -1800f)
			{
				base.ServerDisable();
				return;
			}
			base.Hub.playerStats.DealDamage(new UniversalDamageHandler(this._damagePerTick, DeathTranslations.PocketDecay, null));
			this._damagePerTick += 0.1f;
		}

		protected override void Enabled()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._damagePerTick = this._startingDamage;
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			PlayerEnteringPocketDimensionEventArgs playerEnteringPocketDimensionEventArgs = new PlayerEnteringPocketDimensionEventArgs(base.Hub);
			PlayerEvents.OnEnteringPocketDimension(playerEnteringPocketDimensionEventArgs);
			if (!playerEnteringPocketDimensionEventArgs.IsAllowed)
			{
				return;
			}
			this.CapturePosition = new RelativePosition(fpcRole.FpcModule.Position);
			fpcRole.FpcModule.ServerOverridePosition(Vector3.up * -1998.5f);
			base.Hub.playerEffectsController.EnableEffect<Sinkhole>(0f, false);
			PlayerEvents.OnEnteredPocketDimension(new PlayerEnteredPocketDimensionEventArgs(base.Hub));
		}

		protected override void Disabled()
		{
			base.Disabled();
			base.Hub.playerEffectsController.DisableEffect<Sinkhole>();
		}

		public float ProcessFootstepOverrides(float dis)
		{
			AudioSourcePoolManager.PlayOnTransform(this._footstepSounds.RandomItem<AudioClip>(), base.transform, dis, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
			return this._originalLoudness;
		}

		private const float ActivationHeight = -1998.5f;

		private const float DeactivationHeight = -1800f;

		[SerializeField]
		private float _startingDamage = 1f;

		[SerializeField]
		private AudioClip[] _footstepSounds;

		[SerializeField]
		private float _originalLoudness;

		private float _damagePerTick = 1f;
	}
}
