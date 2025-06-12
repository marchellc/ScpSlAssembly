using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia;

public class AttackCooldownSubEffect : HypothermiaSubEffectBase
{
	private float _prevExpo;

	private static readonly Dictionary<uint, float> MultipliersOfPlayers = new Dictionary<uint, float>();

	[SerializeField]
	private float _cooldownMultiplierPerExposure;

	public override bool IsActive => this._prevExpo > 0f;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += AttackCooldownSubEffect.MultipliersOfPlayers.Clear;
	}

	public static float CurrentAttackCooldownMultiplier(ReferenceHub hub)
	{
		if (!AttackCooldownSubEffect.MultipliersOfPlayers.TryGetValue(hub.networkIdentity.netId, out var value))
		{
			return 1f;
		}
		return value;
	}

	internal override void UpdateEffect(float curExposure)
	{
		if (curExposure != this._prevExpo)
		{
			float value = Mathf.LerpUnclamped(1f, this._cooldownMultiplierPerExposure, curExposure);
			AttackCooldownSubEffect.MultipliersOfPlayers[base.Hub.networkIdentity.netId] = value;
			this._prevExpo = curExposure;
		}
	}
}
