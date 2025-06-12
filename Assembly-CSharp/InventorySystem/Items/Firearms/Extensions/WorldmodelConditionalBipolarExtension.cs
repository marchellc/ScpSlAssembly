using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class WorldmodelConditionalBipolarExtension : MonoBehaviour, IWorldmodelExtension
{
	[SerializeField]
	private ConditionalEvaluator _conditions;

	[SerializeField]
	private BipolarTransform _bipolar;

	public void SetupWorldmodel(FirearmWorldmodel worldmodel)
	{
		this._conditions.InitWorldmodel(worldmodel);
		this._bipolar.Polarity = this._conditions.Evaluate();
	}
}
