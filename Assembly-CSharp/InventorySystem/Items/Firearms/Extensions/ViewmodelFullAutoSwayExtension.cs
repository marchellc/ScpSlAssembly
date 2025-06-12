using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ViewmodelFullAutoSwayExtension : MonoBehaviour, IViewmodelExtension
{
	private const float TriggerIncreaseWeightSpeed = 10f;

	private const float TriggerDecreaseWeightSpeed = 5f;

	private const float RecentlyFiredIncreaseWeightSpeed = 10f;

	private const float RecentlyFiredDecreaseWeightSpeed = 1.5f;

	private const float LoadedIncreaseWeightSpeed = 1.5f;

	private const float LoadedDecreaseWeightSpeed = 5f;

	private const float DefaultAdsMuliplier = 0.1f;

	private const float DefaultHipMuliplier = 1f;

	private SubsequentShotsCounter _shotsCounter;

	private AnimatedFirearmViewmodel _viewmodel;

	private Firearm _firearm;

	[SerializeField]
	private AnimatorLayerMask _recoilLayers;

	[SerializeField]
	private float _adsMultiplier = 0.1f;

	[SerializeField]
	private float _hipMultiplier = 1f;

	private AnimatorStateInfo[] _statesByLayer;

	private float _elapsed;

	private float _weightTrigger;

	private float _weightRecentlyFired;

	private float _weightLoaded;

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._firearm = viewmodel.ParentFirearm;
		this._viewmodel = viewmodel;
		this._statesByLayer = new AnimatorStateInfo[this._recoilLayers.Layers.Length];
		this._shotsCounter = new SubsequentShotsCounter(this._firearm);
		for (int i = 0; i < this._statesByLayer.Length; i++)
		{
			int layer = this._recoilLayers.Layers[i];
			this._statesByLayer[i] = viewmodel.AnimatorStateInfo(layer);
		}
	}

	private void LateUpdate()
	{
		this._shotsCounter.Update();
		ITriggerControllerModule module;
		bool targetIncrease = this._firearm.TryGetModule<ITriggerControllerModule>(out module) && module.TriggerHeld;
		this.UpdateWeight(ref this._weightTrigger, targetIncrease, 10f, 5f);
		bool targetIncrease2 = this._shotsCounter.SubsequentShots > 0;
		this.UpdateWeight(ref this._weightRecentlyFired, targetIncrease2, 10f, 1.5f);
		bool targetIncrease3 = IDisplayableAmmoProviderModule.GetCombinedDisplayAmmo(this._firearm).Total > 0;
		this.UpdateWeight(ref this._weightLoaded, targetIncrease3, 1.5f, 5f);
		float num = this._weightTrigger * this._weightLoaded * this._weightRecentlyFired;
		if (num > 0f)
		{
			this._elapsed += num * Time.deltaTime;
		}
		else
		{
			this._elapsed = 0f;
		}
		IAdsModule module2;
		float t = (this._firearm.TryGetModule<IAdsModule>(out module2) ? module2.AdsAmount : 0f);
		float weight = num * Mathf.Lerp(1f, this._adsMultiplier, t);
		for (int i = 0; i < this._statesByLayer.Length; i++)
		{
			this.UpdateLayer(this._recoilLayers.Layers[i], this._statesByLayer[i], weight);
		}
	}

	private void UpdateWeight(ref float weight, bool targetIncrease, float increaseSpeed, float decreaseSpeed)
	{
		float target = (targetIncrease ? 1 : 0);
		float num = (targetIncrease ? increaseSpeed : decreaseSpeed);
		weight = Mathf.MoveTowards(weight, target, num * Time.deltaTime);
	}

	private void UpdateLayer(int layerId, AnimatorStateInfo state, float weight)
	{
		float time = this._elapsed / state.length;
		this._viewmodel.AnimatorPlay(state.shortNameHash, layerId, time);
		this._viewmodel.AnimatorSetLayerWeight(layerId, weight);
	}
}
