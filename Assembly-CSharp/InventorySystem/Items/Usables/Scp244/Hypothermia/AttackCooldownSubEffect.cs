using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp244.Hypothermia
{
	public class AttackCooldownSubEffect : HypothermiaSubEffectBase
	{
		public override bool IsActive
		{
			get
			{
				return this._prevExpo > 0f;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += AttackCooldownSubEffect.MultipliersOfPlayers.Clear;
		}

		public static float CurrentAttackCooldownMultiplier(ReferenceHub hub)
		{
			float num;
			if (!AttackCooldownSubEffect.MultipliersOfPlayers.TryGetValue(hub.networkIdentity.netId, out num))
			{
				return 1f;
			}
			return num;
		}

		internal override void UpdateEffect(float curExposure)
		{
			if (curExposure == this._prevExpo)
			{
				return;
			}
			float num = Mathf.LerpUnclamped(1f, this._cooldownMultiplierPerExposure, curExposure);
			AttackCooldownSubEffect.MultipliersOfPlayers[base.Hub.networkIdentity.netId] = num;
			this._prevExpo = curExposure;
		}

		private float _prevExpo;

		private static readonly Dictionary<uint, float> MultipliersOfPlayers = new Dictionary<uint, float>();

		[SerializeField]
		private float _cooldownMultiplierPerExposure;
	}
}
