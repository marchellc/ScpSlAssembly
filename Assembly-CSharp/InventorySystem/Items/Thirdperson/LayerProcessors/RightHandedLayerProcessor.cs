using System;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson.LayerProcessors
{
	public class RightHandedLayerProcessor : LayerProcessorBase
	{
		protected override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			return new ThirdpersonLayerWeight(RightHandedLayerProcessor.CalculateWeight(base.TargetModel, layer), false);
		}

		public static float CalculateWeight(AnimatedCharacterModel model, AnimItemLayer3p layer)
		{
			switch (layer)
			{
			case AnimItemLayer3p.Right:
				return Mathf.Lerp(1f, 0.75f, model.WalkLayerWeight);
			case AnimItemLayer3p.Middle:
				return 1f - model.WalkLayerWeight;
			case AnimItemLayer3p.PreMovement:
				return 1f;
			}
			return 0f;
		}

		private const float HandEaseout = 0.75f;
	}
}
