using System;
using System.Collections.Generic;
using AudioPooling;
using GameObjectPools;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HumeShield
{
	public class DynamicHumeShieldController : HumeShieldModuleBase, IPoolSpawnable, IPoolResettable
	{
		public bool IsBlocked
		{
			get
			{
				this._blockersCount -= this._blockers.RemoveWhere(delegate(IHumeShieldBlocker x)
				{
					global::UnityEngine.Object @object = x as global::UnityEngine.Object;
					return (@object != null && @object == null) || x == null || !x.HumeShieldBlocked;
				});
				return this._blockersCount > 0;
			}
		}

		public virtual AudioClip ShieldBreakSound
		{
			get
			{
				return this._shieldBreakSound;
			}
		}

		public override float HsMax
		{
			get
			{
				return this.ShieldOverHealth.Evaluate(this._hp.NormalizedValue);
			}
		}

		public override float HsRegeneration
		{
			get
			{
				if (this._nextRegenTime >= NetworkTime.time || this.IsBlocked)
				{
					return 0f;
				}
				return this.RegenerationRate;
			}
		}

		public override Color? HsWarningColor
		{
			get
			{
				if (!this.IsBlocked)
				{
					return null;
				}
				return new Color?(new Color(1f, 0f, 0f, 0.3f));
			}
		}

		public override bool HideWhenEmpty
		{
			get
			{
				return this._hideWhenEmpty;
			}
		}

		public void AddBlocker(IHumeShieldBlocker blocker)
		{
			if (!this._blockers.Add(blocker))
			{
				return;
			}
			this._blockersCount++;
		}

		public void ResumeRegen()
		{
			this._nextRegenTime = 0.0;
		}

		public override void OnHsValueChanged(float prevValue, float newValue)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			if (newValue > 0f || prevValue <= 0f)
			{
				return;
			}
			if (this.ShieldBreakSound == null)
			{
				return;
			}
			NetworkServer.SendToReady<DynamicHumeShieldController.ShieldBreakMessage>(new DynamicHumeShieldController.ShieldBreakMessage
			{
				Target = base.Owner
			}, 0);
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			PlayerStats playerStats = base.Owner.playerStats;
			this._hp = playerStats.GetModule<HealthStat>();
			playerStats.OnThisPlayerDamaged += this.OnDamaged;
			if (!NetworkServer.active)
			{
				return;
			}
			base.HsCurrent = this.ShieldOverHealth.Evaluate(1f);
		}

		public void ResetObject()
		{
			if (base.Owner != null)
			{
				base.Owner.playerStats.OnThisPlayerDamaged -= this.OnDamaged;
			}
			this.RegenerationCooldown = this._initialRegenCooldown;
			this.RegenerationRate = this._initialRegenRate;
			this._nextRegenTime = 0.0;
			this._blockersCount = 0;
			this._blockers.Clear();
		}

		private void OnDamaged(DamageHandlerBase dhb)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._nextRegenTime = NetworkTime.time + (double)this.RegenerationCooldown;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<DynamicHumeShieldController.ShieldBreakMessage>(new Action<DynamicHumeShieldController.ShieldBreakMessage>(DynamicHumeShieldController.ProcessBreakMessage), true);
			};
		}

		private static void ProcessBreakMessage(DynamicHumeShieldController.ShieldBreakMessage msg)
		{
			if (msg.Target == null)
			{
				return;
			}
			IHumeShieldedRole humeShieldedRole = msg.Target.roleManager.CurrentRole as IHumeShieldedRole;
			if (humeShieldedRole == null)
			{
				return;
			}
			DynamicHumeShieldController dynamicHumeShieldController = humeShieldedRole.HumeShieldModule as DynamicHumeShieldController;
			if (dynamicHumeShieldController == null)
			{
				return;
			}
			AudioSourcePoolManager.PlayOnTransform(dynamicHumeShieldController.ShieldBreakSound, dynamicHumeShieldController.transform, 37f, 1f, FalloffType.Exponential, MixerChannel.NoDucking, 1f);
		}

		private void Awake()
		{
			this._initialRegenCooldown = this.RegenerationCooldown;
			this._initialRegenRate = this.RegenerationRate;
		}

		private const float ShieldBreakSoundRange = 37f;

		private const MixerChannel ShieldBreakSoundChannel = MixerChannel.NoDucking;

		public AnimationCurve ShieldOverHealth;

		public float RegenerationRate;

		public float RegenerationCooldown;

		[SerializeField]
		private AudioClip _shieldBreakSound;

		[SerializeField]
		private bool _hideWhenEmpty;

		private readonly HashSet<IHumeShieldBlocker> _blockers = new HashSet<IHumeShieldBlocker>();

		private HealthStat _hp;

		private double _nextRegenTime;

		private int _blockersCount;

		private float _initialRegenRate;

		private float _initialRegenCooldown;

		public struct ShieldBreakMessage : NetworkMessage
		{
			public ReferenceHub Target;
		}
	}
}
