using InventorySystem.Items.Firearms.Extensions;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class ViewmodelConditionalLayerExtension : MonoBehaviour, IViewmodelExtension
{
	private Firearm _firearm;

	[SerializeField]
	private ConditionalEvaluator _condition;

	[SerializeField]
	private AnimatorConditionalOverride _layer;

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._firearm = viewmodel.ParentFirearm;
		this._condition.InitInstance(this._firearm);
	}

	private void LateUpdate()
	{
		this._layer.Update(this._firearm, this._condition.Evaluate());
	}
}
