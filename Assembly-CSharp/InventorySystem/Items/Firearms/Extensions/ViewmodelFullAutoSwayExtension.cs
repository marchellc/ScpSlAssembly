using System;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class ViewmodelFullAutoSwayExtension : MonoBehaviour, IViewmodelExtension
	{
		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._firearm = viewmodel.ParentFirearm;
			this._viewmodel = viewmodel;
			this._statesByLayer = new AnimatorStateInfo[this._recoilLayers.Layers.Length];
			this._shotsCounter = new SubsequentShotsCounter(this._firearm, 1f, 0.1f, 0.4f);
			for (int i = 0; i < this._statesByLayer.Length; i++)
			{
				int num = this._recoilLayers.Layers[i];
				this._statesByLayer[i] = viewmodel.AnimatorStateInfo(num);
			}
		}

		private void LateUpdate()
		{
			this._shotsCounter.Update();
			ITriggerControllerModule triggerControllerModule;
			bool flag = this._firearm.TryGetModule(out triggerControllerModule, true) && triggerControllerModule.TriggerHeld;
			this.UpdateWeight(ref this._weightTrigger, flag, 10f, 5f);
			bool flag2 = this._shotsCounter.SubsequentShots > 0;
			this.UpdateWeight(ref this._weightRecentlyFired, flag2, 10f, 1.5f);
			bool flag3 = IDisplayableAmmoProviderModule.GetCombinedDisplayAmmo(this._firearm).Total > 0;
			this.UpdateWeight(ref this._weightLoaded, flag3, 1.5f, 5f);
			float num = this._weightTrigger * this._weightLoaded * this._weightRecentlyFired;
			if (num > 0f)
			{
				this._elapsed += num * Time.deltaTime;
			}
			else
			{
				this._elapsed = 0f;
			}
			IAdsModule adsModule;
			float num2 = (this._firearm.TryGetModule(out adsModule, true) ? adsModule.AdsAmount : 0f);
			float num3 = num * Mathf.Lerp(1f, this._adsMultiplier, num2);
			for (int i = 0; i < this._statesByLayer.Length; i++)
			{
				this.UpdateLayer(this._recoilLayers.Layers[i], this._statesByLayer[i], num3);
			}
		}

		private void UpdateWeight(ref float weight, bool targetIncrease, float increaseSpeed, float decreaseSpeed)
		{
			float num = (float)(targetIncrease ? 1 : 0);
			float num2 = (targetIncrease ? increaseSpeed : decreaseSpeed);
			weight = Mathf.MoveTowards(weight, num, num2 * Time.deltaTime);
		}

		private void UpdateLayer(int layerId, AnimatorStateInfo state, float weight)
		{
			float num = this._elapsed / state.length;
			this._viewmodel.AnimatorPlay(state.shortNameHash, layerId, num);
			this._viewmodel.AnimatorSetLayerWeight(layerId, weight);
		}

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
	}
}
