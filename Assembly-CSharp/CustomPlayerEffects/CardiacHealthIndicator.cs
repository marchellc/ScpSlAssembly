using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomPlayerEffects
{
	public class CardiacHealthIndicator : SubEffectBase
	{
		public override bool IsActive
		{
			get
			{
				return base.MainEffect.IsEnabled;
			}
		}

		public override void DisableEffect()
		{
			base.DisableEffect();
		}

		internal override void UpdateEffect()
		{
			base.UpdateEffect();
		}

		internal override void Init(StatusEffectBase mainEffect)
		{
			base.Init(mainEffect);
		}

		[SerializeField]
		private AnimationCurve _healthToWeight;

		[SerializeField]
		private Volume _ppv;

		[SerializeField]
		private float _speedMultiplier;
	}
}
