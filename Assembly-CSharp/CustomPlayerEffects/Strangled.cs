using System;
using System.Runtime.CompilerServices;
using CursorManagement;
using InventorySystem.Items;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerStatsSystem;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Strangled : StatusEffectBase, ISoundtrackMutingEffect, IMovementInputOverride, IStaminaModifier, IMovementSpeedModifier, ICursorOverride, IInteractionBlocker
	{
		public bool MuteSoundtrack
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool MovementOverrideActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public Vector3 MovementOverrideDirection { get; private set; }

		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return base.IsEnabled && base.IsLocalPlayer;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return 2.1474836E+09f;
			}
		}

		public float MovementSpeedLimit { get; private set; }

		public CursorOverrideMode CursorOverride
		{
			get
			{
				return CursorOverrideMode.NoOverride;
			}
		}

		public bool LockMovement
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		private float DamageRate
		{
			get
			{
				double elapsedSeconds = this.ElapsedSeconds;
				if (elapsedSeconds < 0.0)
				{
					return 0f;
				}
				double num = 5.0 * elapsedSeconds;
				return (float)(5.0 + num);
			}
		}

		public float EstimatedTimeToKill
		{
			get
			{
				StatBase module = base.Hub.playerStats.GetModule<HealthStat>();
				AhpStat module2 = base.Hub.playerStats.GetModule<AhpStat>();
				float num = module.CurValue + module2.CurValue;
				float damageRate = this.DamageRate;
				float num2 = damageRate * damageRate;
				return (-damageRate + Mathf.Sqrt(num2 + 10f * num)) / 5f;
			}
		}

		public double ElapsedSeconds
		{
			get
			{
				return NetworkTime.time - this._startTime;
			}
		}

		public BlockedInteraction BlockedInteractions
		{
			get
			{
				return (BlockedInteraction)50;
			}
		}

		public bool CanBeCleared
		{
			get
			{
				return !base.IsEnabled;
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			this._startTime = NetworkTime.time;
			this.MovementSpeedLimit = 0f;
			this.MovementOverrideDirection = Vector3.zero;
			base.Hub.interCoordinator.AddBlocker(this);
			if (!base.Hub.isLocalPlayer)
			{
				return;
			}
			this._overrideRegistered = true;
			CursorManager.Register(this);
		}

		protected override void Disabled()
		{
			base.Disabled();
			if (!base.Hub.isLocalPlayer)
			{
				return;
			}
			this._overrideRegistered = false;
			CursorManager.Unregister(this);
		}

		protected override void OnEffectUpdate()
		{
			base.OnEffectUpdate();
			if (base.IsLocalPlayer)
			{
				this.UpdateInputOverride();
			}
			if (NetworkServer.active && !this.ServerUpdate())
			{
				this.DisableEffect();
			}
		}

		private bool ServerUpdate()
		{
			ReferenceHub referenceHub;
			if (!this.TryUpdateAttacker(out referenceHub))
			{
				return false;
			}
			base.Hub.inventory.ServerSelectItem(0);
			base.Hub.playerStats.DealDamage(new Scp3114DamageHandler(referenceHub, this.DamageRate * Time.deltaTime, Scp3114DamageHandler.HandlerType.Strangulation));
			return true;
		}

		private void UpdateInputOverride()
		{
			ReferenceHub referenceHub;
			if (!this.TryUpdateAttacker(out referenceHub))
			{
				return;
			}
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			FirstPersonMovementModule fpcModule = fpcRole.FpcModule;
			fpcRole.LookAtPoint(this._strangleTarget.AttackerPosition.Position, Time.deltaTime * 20f);
			Vector3 vector = this._strangleTarget.TargetPosition.Position - fpcModule.Position;
			float num = vector.MagnitudeIgnoreY();
			Vector3 vector2 = vector / num;
			if (Mathf.Approximately(num, 0f))
			{
				return;
			}
			this.MovementOverrideDirection = vector2;
			this.MovementSpeedLimit = Mathf.Min(5f, num / Time.deltaTime);
		}

		public bool TryUpdateAttacker(out ReferenceHub attacker)
		{
			if (this._hasCache && this._cachedHub != null && this.<TryUpdateAttacker>g__CheckPlayer|50_0(this._cachedHub))
			{
				attacker = this._cachedHub;
				return true;
			}
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (this.<TryUpdateAttacker>g__CheckPlayer|50_0(referenceHub))
				{
					attacker = referenceHub;
					this._hasCache = true;
					return true;
				}
			}
			this._hasCache = false;
			attacker = null;
			return false;
		}

		private void OnDestroy()
		{
			if (!this._overrideRegistered)
			{
				return;
			}
			CursorManager.Unregister(this);
		}

		[CompilerGenerated]
		private bool <TryUpdateAttacker>g__CheckPlayer|50_0(ReferenceHub hub)
		{
			Scp3114Role scp3114Role = hub.roleManager.CurrentRole as Scp3114Role;
			if (scp3114Role == null)
			{
				return false;
			}
			Scp3114Strangle scp3114Strangle;
			if (!scp3114Role.SubroutineModule.TryGetSubroutine<Scp3114Strangle>(out scp3114Strangle))
			{
				return false;
			}
			if (scp3114Strangle.SyncTarget == null)
			{
				return false;
			}
			if (scp3114Strangle.SyncTarget.Value.Target != base.Hub)
			{
				return false;
			}
			this._cachedHub = hub;
			this._strangleTarget = scp3114Strangle.SyncTarget.Value;
			return true;
		}

		private bool _hasCache;

		private bool _overrideRegistered;

		private double _startTime;

		private ReferenceHub _cachedHub;

		private Scp3114Strangle.StrangleTarget _strangleTarget;

		private const float MaxMovementSpeed = 5f;

		private const float LookLerp = 20f;

		private const float StartDamageRate = 5f;

		private const float DamageRatePerSec = 5f;
	}
}
