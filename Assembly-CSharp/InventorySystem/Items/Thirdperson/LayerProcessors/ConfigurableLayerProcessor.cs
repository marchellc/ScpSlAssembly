using System;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson.LayerProcessors
{
	public class ConfigurableLayerProcessor : LayerProcessorBase
	{
		private float WalkWeight
		{
			get
			{
				return base.TargetModel.WalkLayerWeight;
			}
		}

		protected override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			float num;
			switch (layer)
			{
			case AnimItemLayer3p.Left:
				if (this._rightHandOnly)
				{
					goto IL_0062;
				}
				break;
			case AnimItemLayer3p.Right:
				break;
			case AnimItemLayer3p.Middle:
				num = Mathf.Lerp(this._chestStationaryIntensity, this._chestWalkIntensity, this.WalkWeight);
				goto IL_0068;
			case AnimItemLayer3p.Additive:
				goto IL_0062;
			case AnimItemLayer3p.PreMovement:
				num = 1f;
				goto IL_0068;
			default:
				goto IL_0062;
			}
			num = Mathf.Lerp(this._handStationaryIntensity, this._handWalkIntensity, this.WalkWeight);
			goto IL_0068;
			IL_0062:
			num = 0f;
			IL_0068:
			return new ThirdpersonLayerWeight(num, !this._overlayBlockMask.Contains(layer));
		}

		[SerializeField]
		private bool _rightHandOnly = true;

		[SerializeField]
		private float _handStationaryIntensity = 1f;

		[SerializeField]
		private float _handWalkIntensity = 0.5f;

		[SerializeField]
		private float _chestStationaryIntensity = 1f;

		[SerializeField]
		private float _chestWalkIntensity;

		[SerializeField]
		private AnimItemLayer3p[] _overlayBlockMask = new AnimItemLayer3p[] { AnimItemLayer3p.Right };
	}
}
