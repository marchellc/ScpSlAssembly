using System.Diagnostics;
using System.Runtime.InteropServices;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class HumeShieldSubEffect : HypothermiaSubEffectBase, IHumeShieldBlocker
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct HumeBlockMsg : NetworkMessage
	{
	}

	[SerializeField]
	private AudioClip[] _freezeSounds;

	[SerializeField]
	private float _hsSustainTime;

	[SerializeField]
	private float _hsDecreaseStartTime;

	[SerializeField]
	private float _hsDecreaseAbsolute;

	[SerializeField]
	private float _hsDecreasePerExposure;

	private float _decreaseTimer;

	private static HumeShieldSubEffect _localEffect;

	private readonly Stopwatch _cooldownSw = Stopwatch.StartNew();

	private readonly Stopwatch _sustainSw = new Stopwatch();

	public override bool IsActive
	{
		get
		{
			if (!this.HumeShieldBlocked)
			{
				if (this._sustainSw.IsRunning)
				{
					return this._sustainSw.Elapsed.TotalSeconds < (double)this._hsSustainTime;
				}
				return false;
			}
			return true;
		}
	}

	public bool HumeShieldBlocked { get; private set; }

	[RuntimeInitializeOnLoadMethod]
	private static void Register()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<HumeBlockMsg>(OnMessageReceived);
		};
	}

	private static void OnMessageReceived(HumeBlockMsg msg)
	{
		if (!(HumeShieldSubEffect._localEffect == null))
		{
			HumeShieldSubEffect._localEffect.ReceiveHumeBlockMessage();
		}
	}

	internal override void Init(StatusEffectBase mainEffect)
	{
		base.Init(mainEffect);
		if (mainEffect.IsLocalPlayer)
		{
			HumeShieldSubEffect._localEffect = this;
		}
	}

	internal override void UpdateEffect(float curExposure)
	{
		if (!NetworkServer.active)
		{
			if (this.HumeShieldBlocked && curExposure <= 0f)
			{
				this.HumeShieldBlocked = false;
			}
			return;
		}
		bool humeShieldBlocked = this.HumeShieldBlocked;
		this.HumeShieldBlocked = this.UpdateHumeShield(curExposure);
		if (this.HumeShieldBlocked)
		{
			if (!humeShieldBlocked)
			{
				base.Hub.networkIdentity.connectionToClient.Send(default(HumeBlockMsg));
			}
		}
		else
		{
			this._decreaseTimer = 0f;
		}
	}

	private bool UpdateHumeShield(float expo)
	{
		if (expo == 0f || !this.TryGetController(out var ctrl) || base.Hub.characterClassManager.GodMode)
		{
			return false;
		}
		ctrl.AddBlocker(this);
		this._sustainSw.Restart();
		this._decreaseTimer += expo * Time.deltaTime;
		if (this._decreaseTimer < this._hsDecreaseStartTime)
		{
			return true;
		}
		ctrl.HsCurrent -= (expo * this._hsDecreasePerExposure + this._hsDecreaseAbsolute) * Time.deltaTime;
		return true;
	}

	private void ReceiveHumeBlockMessage()
	{
		if (this.TryGetController(out var ctrl))
		{
			this.HumeShieldBlocked = true;
			ctrl.AddBlocker(this);
			if (!(this._cooldownSw.Elapsed.TotalSeconds < (double)this._hsSustainTime))
			{
				AudioSourcePoolManager.Play2D(this._freezeSounds.RandomItem());
				this._cooldownSw.Restart();
			}
		}
	}

	private bool TryGetController(out DynamicHumeShieldController ctrl)
	{
		if (!(base.Hub.roleManager.CurrentRole is IHumeShieldedRole { HumeShieldModule: DynamicHumeShieldController humeShieldModule }))
		{
			ctrl = null;
			return false;
		}
		ctrl = humeShieldModule;
		return true;
	}
}
