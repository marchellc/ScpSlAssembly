using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ViewmodelRecoilLayerScalerExtension : MonoBehaviour, IViewmodelExtension
{
	private Firearm _fa;

	private AnimatedViewmodelBase _viewmodel;

	private float _lastShotBonus;

	private SubsequentShotsCounter _shotsCounter;

	[SerializeField]
	private AnimatorLayerMask _recoilLayers;

	[SerializeField]
	private float _postShotReturnSpeed;

	[SerializeField]
	private AnimationCurve _weightOverScale;

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._viewmodel = viewmodel;
		this._fa = viewmodel.ParentFirearm;
		this._shotsCounter = new SubsequentShotsCounter(this._fa, 0f, 0f, 0f);
		this._shotsCounter.OnShotRecorded += delegate
		{
			this._lastShotBonus = this._shotsCounter.SubsequentShots;
		};
	}

	private void Update()
	{
		float num = this._fa.AttachmentsValue(AttachmentParam.OverallRecoilMultiplier);
		this._lastShotBonus = Mathf.MoveTowards(this._lastShotBonus, 1f, Time.deltaTime * this._postShotReturnSpeed);
		float num2 = this._weightOverScale.Evaluate(this._lastShotBonus * num);
		this._recoilLayers.SetWeight(this._viewmodel.AnimatorSetLayerWeight, num2);
		int[] layers = this._recoilLayers.Layers;
		foreach (int layer in layers)
		{
			this._viewmodel.AnimatorSetLayerWeight(layer, num2);
		}
		this._shotsCounter.Update();
	}
}
