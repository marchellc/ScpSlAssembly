using System.Collections.Generic;
using AudioPooling;
using GameObjectPools;
using Mirror;
using PlayerRoles.RoleAssign;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HumeShield;

public class DynamicHumeShieldController : HumeShieldModuleBase, IPoolSpawnable, IPoolResettable
{
	public struct ShieldBreakMessage : NetworkMessage
	{
		public ReferenceHub Target;
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

	public bool IsBlocked
	{
		get
		{
			this._blockersCount -= this._blockers.RemoveWhere((IHumeShieldBlocker x) => (x is Object obj && obj == null) || x == null || !x.HumeShieldBlocked);
			return this._blockersCount > 0;
		}
	}

	public virtual AudioClip ShieldBreakSound => this._shieldBreakSound;

	public virtual bool ServerPlayBreakSound
	{
		get
		{
			if (base.Owner.IsSCP())
			{
				return this.ShieldBreakSound != null;
			}
			return false;
		}
	}

	public override float HsMax
	{
		get
		{
			float num = this.ShieldOverHealth.Evaluate(this._hp.NormalizedValue);
			if (RoleAssigner.ScpsOverflowing)
			{
				num *= RoleAssigner.ScpOverflowMaxHsMultiplier;
			}
			return num;
		}
	}

	public override float HsRegeneration
	{
		get
		{
			if (!(this._nextRegenTime < NetworkTime.time) || this.IsBlocked)
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
			return new Color(1f, 0f, 0f, 0.3f);
		}
	}

	public override bool HideWhenEmpty => this._hideWhenEmpty;

	public void AddBlocker(IHumeShieldBlocker blocker)
	{
		if (this._blockers.Add(blocker))
		{
			this._blockersCount++;
		}
	}

	public void ResumeRegen()
	{
		this._nextRegenTime = 0.0;
	}

	public override void OnHsValueChanged(float prevValue, float newValue)
	{
		if (NetworkServer.active && !(newValue > 0f) && !(prevValue <= 0f) && this.ServerPlayBreakSound)
		{
			NetworkServer.SendToReady(new ShieldBreakMessage
			{
				Target = base.Owner
			});
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		PlayerStats playerStats = base.Owner.playerStats;
		this._hp = playerStats.GetModule<HealthStat>();
		playerStats.OnThisPlayerDamaged += OnDamaged;
		if (NetworkServer.active)
		{
			base.HsCurrent = this.ShieldOverHealth.Evaluate(1f);
		}
	}

	public void ResetObject()
	{
		if (base.Owner != null)
		{
			base.Owner.playerStats.OnThisPlayerDamaged -= OnDamaged;
		}
		this.RegenerationCooldown = this._initialRegenCooldown;
		this.RegenerationRate = this._initialRegenRate;
		this._nextRegenTime = 0.0;
		this._blockersCount = 0;
		this._blockers.Clear();
	}

	private void OnDamaged(DamageHandlerBase dhb)
	{
		if (NetworkServer.active)
		{
			this._nextRegenTime = NetworkTime.time + (double)this.RegenerationCooldown;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<ShieldBreakMessage>(ProcessBreakMessage);
		};
	}

	private static void ProcessBreakMessage(ShieldBreakMessage msg)
	{
		if (!(msg.Target == null) && msg.Target.roleManager.CurrentRole is IHumeShieldedRole { HumeShieldModule: DynamicHumeShieldController humeShieldModule })
		{
			AudioSourcePoolManager.PlayOnTransform(humeShieldModule.ShieldBreakSound, humeShieldModule.transform, 37f, 1f, FalloffType.Exponential, MixerChannel.NoDucking);
		}
	}

	private void Awake()
	{
		this._initialRegenCooldown = this.RegenerationCooldown;
		this._initialRegenRate = this.RegenerationRate;
	}
}
