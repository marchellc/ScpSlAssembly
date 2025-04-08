using System;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson.LayerProcessors
{
	public class DualHandedLayerProcessor : LayerProcessorBase
	{
		protected override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			return new ThirdpersonLayerWeight(DualHandedLayerProcessor.CalculateWeight(base.TargetModel, layer), false);
		}

		public static float CalculateWeight(AnimatedCharacterModel model, AnimItemLayer3p layer)
		{
			switch (layer)
			{
			case AnimItemLayer3p.Left:
			case AnimItemLayer3p.Right:
				return 1f;
			case AnimItemLayer3p.Middle:
				return Mathf.Lerp(1f, 0.3f, model.WalkLayerWeight);
			case AnimItemLayer3p.PreMovement:
				return 1f;
			}
			return 0f;
		}

		private const float ChestEaseout = 0.3f;
	}
}
