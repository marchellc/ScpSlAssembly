using System;
using System.Diagnostics;
using AudioPooling;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.PlayableScps.HumeShield;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia
{
	public class HumeShieldSubEffect : HypothermiaSubEffectBase, IHumeShieldBlocker
	{
		public override bool IsActive
		{
			get
			{
				return this.HumeShieldBlocked || (this._sustainSw.IsRunning && this._sustainSw.Elapsed.TotalSeconds < (double)this._hsSustainTime);
			}
		}

		public bool HumeShieldBlocked { get; private set; }

		[RuntimeInitializeOnLoadMethod]
		private static void Register()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<HumeShieldSubEffect.HumeBlockMsg>(new Action<HumeShieldSubEffect.HumeBlockMsg>(HumeShieldSubEffect.OnMessageReceived), true);
			};
		}

		private static void OnMessageReceived(HumeShieldSubEffect.HumeBlockMsg msg)
		{
			if (HumeShieldSubEffect._localEffect == null)
			{
				return;
			}
			HumeShieldSubEffect._localEffect.ReceiveHumeBlockMessage();
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
			if (!this.HumeShieldBlocked)
			{
				this._decreaseTimer = 0f;
				return;
			}
			if (humeShieldBlocked)
			{
				return;
			}
			base.Hub.networkIdentity.connectionToClient.Send<HumeShieldSubEffect.HumeBlockMsg>(default(HumeShieldSubEffect.HumeBlockMsg), 0);
		}

		private bool UpdateHumeShield(float expo)
		{
			DynamicHumeShieldController dynamicHumeShieldController;
			if (expo == 0f || !this.TryGetController(out dynamicHumeShieldController) || base.Hub.characterClassManager.GodMode)
			{
				return false;
			}
			dynamicHumeShieldController.AddBlocker(this);
			this._sustainSw.Restart();
			this._decreaseTimer += expo * Time.deltaTime;
			if (this._decreaseTimer < this._hsDecreaseStartTime)
			{
				return true;
			}
			dynamicHumeShieldController.HsCurrent -= (expo * this._hsDecreasePerExposure + this._hsDecreaseAbsolute) * Time.deltaTime;
			return true;
		}

		private void ReceiveHumeBlockMessage()
		{
			DynamicHumeShieldController dynamicHumeShieldController;
			if (!this.TryGetController(out dynamicHumeShieldController))
			{
				return;
			}
			this.HumeShieldBlocked = true;
			dynamicHumeShieldController.AddBlocker(this);
			if (this._cooldownSw.Elapsed.TotalSeconds < (double)this._hsSustainTime)
			{
				return;
			}
			AudioSourcePoolManager.Play2D(this._freezeSounds.RandomItem<AudioClip>(), 1f, MixerChannel.DefaultSfx, 1f);
			this._cooldownSw.Restart();
		}

		private bool TryGetController(out DynamicHumeShieldController ctrl)
		{
			IHumeShieldedRole humeShieldedRole = base.Hub.roleManager.CurrentRole as IHumeShieldedRole;
			if (humeShieldedRole != null)
			{
				DynamicHumeShieldController dynamicHumeShieldController = humeShieldedRole.HumeShieldModule as DynamicHumeShieldController;
				if (dynamicHumeShieldController != null)
				{
					ctrl = dynamicHumeShieldController;
					return true;
				}
			}
			ctrl = null;
			return false;
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

		public struct HumeBlockMsg : NetworkMessage
		{
		}
	}
}
