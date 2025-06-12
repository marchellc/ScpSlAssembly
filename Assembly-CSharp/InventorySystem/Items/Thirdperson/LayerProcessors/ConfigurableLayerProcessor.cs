using UnityEngine;

namespace InventorySystem.Items.Thirdperson.LayerProcessors;

public class ConfigurableLayerProcessor : LayerProcessorBase
{
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
	private AnimItemLayer3p[] _overlayBlockMask = new AnimItemLayer3p[1] { AnimItemLayer3p.Right };

	private float WalkWeight => base.TargetModel.WalkLayerWeight;

	protected override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
	{
		float weight;
		switch (layer)
		{
		case AnimItemLayer3p.PreMovement:
			weight = 1f;
			break;
		case AnimItemLayer3p.Left:
			if (!this._rightHandOnly)
			{
				goto case AnimItemLayer3p.Right;
			}
			goto default;
		case AnimItemLayer3p.Right:
			weight = Mathf.Lerp(this._handStationaryIntensity, this._handWalkIntensity, this.WalkWeight);
			break;
		case AnimItemLayer3p.Middle:
			weight = Mathf.Lerp(this._chestStationaryIntensity, this._chestWalkIntensity, this.WalkWeight);
			break;
		default:
			weight = 0f;
			break;
		}
		return new ThirdpersonLayerWeight(weight, !this._overlayBlockMask.Contains(layer));
	}
}
