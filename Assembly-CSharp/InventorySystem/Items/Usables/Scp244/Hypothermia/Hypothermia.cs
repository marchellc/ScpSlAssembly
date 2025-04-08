using System;
using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Searching;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia
{
	public class Hypothermia : ParentEffectBase<HypothermiaSubEffectBase>, IWeaponModifierPlayerEffect, ISoundtrackMutingEffect, ISearchTimeModifier, IMovementSpeedModifier
	{
		public bool MuteSoundtrack { get; private set; }

		public bool ParamsActive { get; private set; }

		public bool MovementModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float MovementSpeedMultiplier { get; private set; }

		public float MovementSpeedLimit { get; private set; }

		public float ProcessSearchTime(float val)
		{
			HypothermiaSubEffectBase[] subEffects = base.SubEffects;
			for (int i = 0; i < subEffects.Length; i++)
			{
				ISearchTimeModifier searchTimeModifier = subEffects[i] as ISearchTimeModifier;
				if (searchTimeModifier != null)
				{
					val = searchTimeModifier.ProcessSearchTime(val);
				}
			}
			return val;
		}

		protected override void Update()
		{
			base.Update();
			this._curExposure = 0f;
			this.CheckForceState();
			if (!Vitality.CheckPlayer(base.Hub) && !SpawnProtected.CheckPlayer(base.Hub))
			{
				this.UpdateExposure();
			}
			bool flag = false;
			this.ParamsActive = false;
			this.MuteSoundtrack = false;
			this.MovementSpeedLimit = float.MaxValue;
			this.MovementSpeedMultiplier = 1f;
			foreach (HypothermiaSubEffectBase hypothermiaSubEffectBase in base.SubEffects)
			{
				flag |= hypothermiaSubEffectBase.IsActive;
				this.UpdateSubEffect(hypothermiaSubEffectBase, this._curExposure);
			}
			if (NetworkServer.active)
			{
				float num = (flag ? (1f + this._curExposure / 0.1f) : 0f);
				base.Intensity = (byte)Mathf.RoundToInt(Mathf.Min(num, 255f));
			}
		}

		private void UpdateSubEffect(HypothermiaSubEffectBase subEffect, float curExposure)
		{
			subEffect.UpdateEffect(curExposure);
			IWeaponModifierPlayerEffect weaponModifierPlayerEffect = subEffect as IWeaponModifierPlayerEffect;
			if (weaponModifierPlayerEffect != null)
			{
				this.ParamsActive |= weaponModifierPlayerEffect.ParamsActive;
				this._weaponModifier = weaponModifierPlayerEffect;
			}
			ISoundtrackMutingEffect soundtrackMutingEffect = subEffect as ISoundtrackMutingEffect;
			if (soundtrackMutingEffect != null)
			{
				this.MuteSoundtrack |= soundtrackMutingEffect.MuteSoundtrack;
			}
			IMovementSpeedModifier movementSpeedModifier = subEffect as IMovementSpeedModifier;
			if (movementSpeedModifier != null)
			{
				this.MovementSpeedLimit = Mathf.Min(this.MovementSpeedLimit, movementSpeedModifier.MovementSpeedLimit);
				this.MovementSpeedMultiplier *= movementSpeedModifier.MovementSpeedMultiplier;
			}
		}

		private void UpdateExposure()
		{
			foreach (Scp244DeployablePickup scp244DeployablePickup in Scp244DeployablePickup.Instances)
			{
				this._curExposure += scp244DeployablePickup.FogPercentForPoint(base.Hub.PlayerCameraReference.position);
			}
			if (this._isForced)
			{
				this._curExposure += this._forcedExposure;
			}
		}

		public bool TryGetWeaponParam(AttachmentParam param, out float val)
		{
			return this._weaponModifier.TryGetWeaponParam(param, out val);
		}

		private void CheckForceState()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			this._isForced = base.TimeLeft > 0f;
			if (this._isForced && !this._wasForcedLastFrame)
			{
				this._forcedExposure = (float)base.Intensity / 100f;
				new Hypothermia.ForcedHypothermiaMessage
				{
					IsForced = true,
					PlayerHub = base.Hub,
					Exposure = this._forcedExposure
				}.SendToAuthenticated(0);
			}
			else if (!this._isForced && this._wasForcedLastFrame)
			{
				this._forcedExposure = 0f;
				new Hypothermia.ForcedHypothermiaMessage
				{
					IsForced = false,
					PlayerHub = base.Hub,
					Exposure = 0f
				}.SendToAuthenticated(0);
			}
			this._wasForcedLastFrame = this._isForced;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<Hypothermia.ForcedHypothermiaMessage>(new Action<Hypothermia.ForcedHypothermiaMessage>(Hypothermia.ClientReceiveForcedMessage), true);
			};
		}

		private static void ClientReceiveForcedMessage(Hypothermia.ForcedHypothermiaMessage message)
		{
			Hypothermia effect = message.PlayerHub.playerEffectsController.GetEffect<Hypothermia>();
			effect._isForced = message.IsForced;
			effect._forcedExposure = message.Exposure;
		}

		private float _curExposure;

		private IWeaponModifierPlayerEffect _weaponModifier;

		private bool _isForced;

		private float _forcedExposure;

		private bool _wasForcedLastFrame;

		private const float IntensityRatio = 0.1f;

		public struct ForcedHypothermiaMessage : NetworkMessage
		{
			public bool IsForced;

			public float Exposure;

			public ReferenceHub PlayerHub;
		}
	}
}
