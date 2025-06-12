using CustomPlayerEffects;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Searching;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.Networking;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class Hypothermia : ParentEffectBase<HypothermiaSubEffectBase>, IWeaponModifierPlayerEffect, ISoundtrackMutingEffect, ISearchTimeModifier, IMovementSpeedModifier
{
	public struct ForcedHypothermiaMessage : NetworkMessage
	{
		public bool IsForced;

		public float Exposure;

		public ReferenceHub PlayerHub;
	}

	private float _curExposure;

	private IWeaponModifierPlayerEffect _weaponModifier;

	private bool _isForced;

	private float _forcedExposure;

	private bool _wasForcedLastFrame;

	private const float IntensityRatio = 0.1f;

	public bool MuteSoundtrack { get; private set; }

	public bool ParamsActive { get; private set; }

	public bool MovementModifierActive => base.IsEnabled;

	public float MovementSpeedMultiplier { get; private set; }

	public float MovementSpeedLimit { get; private set; }

	public float ProcessSearchTime(float val)
	{
		HypothermiaSubEffectBase[] subEffects = base.SubEffects;
		for (int i = 0; i < subEffects.Length; i++)
		{
			if (subEffects[i] is ISearchTimeModifier searchTimeModifier)
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
		HypothermiaSubEffectBase[] subEffects = base.SubEffects;
		foreach (HypothermiaSubEffectBase hypothermiaSubEffectBase in subEffects)
		{
			flag |= hypothermiaSubEffectBase.IsActive;
			this.UpdateSubEffect(hypothermiaSubEffectBase, this._curExposure);
		}
		if (NetworkServer.active)
		{
			float a = (flag ? (1f + this._curExposure / 0.1f) : 0f);
			base.Intensity = (byte)Mathf.RoundToInt(Mathf.Min(a, 255f));
		}
	}

	private void UpdateSubEffect(HypothermiaSubEffectBase subEffect, float curExposure)
	{
		subEffect.UpdateEffect(curExposure);
		if (subEffect is IWeaponModifierPlayerEffect weaponModifierPlayerEffect)
		{
			this.ParamsActive |= weaponModifierPlayerEffect.ParamsActive;
			this._weaponModifier = weaponModifierPlayerEffect;
		}
		if (subEffect is ISoundtrackMutingEffect soundtrackMutingEffect)
		{
			this.MuteSoundtrack |= soundtrackMutingEffect.MuteSoundtrack;
		}
		if (subEffect is IMovementSpeedModifier movementSpeedModifier)
		{
			this.MovementSpeedLimit = Mathf.Min(this.MovementSpeedLimit, movementSpeedModifier.MovementSpeedLimit);
			this.MovementSpeedMultiplier *= movementSpeedModifier.MovementSpeedMultiplier;
		}
	}

	private void UpdateExposure()
	{
		foreach (Scp244DeployablePickup instance in Scp244DeployablePickup.Instances)
		{
			this._curExposure += instance.FogPercentForPoint(base.Hub.PlayerCameraReference.position);
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
		if (NetworkServer.active)
		{
			this._isForced = base.TimeLeft > 0f;
			if (this._isForced && !this._wasForcedLastFrame)
			{
				this._forcedExposure = (float)(int)base.Intensity / 100f;
				new ForcedHypothermiaMessage
				{
					IsForced = true,
					PlayerHub = base.Hub,
					Exposure = this._forcedExposure
				}.SendToAuthenticated();
			}
			else if (!this._isForced && this._wasForcedLastFrame)
			{
				this._forcedExposure = 0f;
				new ForcedHypothermiaMessage
				{
					IsForced = false,
					PlayerHub = base.Hub,
					Exposure = 0f
				}.SendToAuthenticated();
			}
			this._wasForcedLastFrame = this._isForced;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkClient.ReplaceHandler<ForcedHypothermiaMessage>(ClientReceiveForcedMessage);
		};
	}

	private static void ClientReceiveForcedMessage(ForcedHypothermiaMessage message)
	{
		Hypothermia effect = message.PlayerHub.playerEffectsController.GetEffect<Hypothermia>();
		effect._isForced = message.IsForced;
		effect._forcedExposure = message.Exposure;
	}
}
