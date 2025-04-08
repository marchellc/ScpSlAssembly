using System;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson.LayerProcessors
{
	public class HybridLayerProcessor : LayerProcessorBase
	{
		public void SetDualHandBlend(float blendAmount)
		{
			this._blend = Mathf.Clamp01(blendAmount);
		}

		protected override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			if (this._blend <= 0f)
			{
				return this.ZeroBlendWeight(layer);
			}
			if (this._blend >= 1f)
			{
				return this.FullBlendWeight(layer);
			}
			ThirdpersonLayerWeight thirdpersonLayerWeight = this.ZeroBlendWeight(layer);
			ThirdpersonLayerWeight thirdpersonLayerWeight2 = this.FullBlendWeight(layer);
			return ThirdpersonLayerWeight.Lerp(thirdpersonLayerWeight, thirdpersonLayerWeight2, this._blend);
		}

		private ThirdpersonLayerWeight ZeroBlendWeight(AnimItemLayer3p layer)
		{
			return new ThirdpersonLayerWeight(RightHandedLayerProcessor.CalculateWeight(base.TargetModel, layer), layer != AnimItemLayer3p.Right);
		}

		private ThirdpersonLayerWeight FullBlendWeight(AnimItemLayer3p layer)
		{
			return new ThirdpersonLayerWeight(DualHandedLayerProcessor.CalculateWeight(base.TargetModel, layer), false);
		}

		private float _blend;
	}
}
