using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	[Serializable]
	public class AnimatorConditionalOverride
	{
		private bool IsReloading(Firearm firearm)
		{
			IReloaderModule reloaderModule;
			return firearm.TryGetModule(out reloaderModule, true) && reloaderModule.IsReloadingOrUnloading;
		}

		private bool IsHandling(Firearm firearm)
		{
			AnimatedViewmodelBase clientViewmodelInstance = firearm.ClientViewmodelInstance;
			int num = clientViewmodelInstance.AnimatorGetLayerCount();
			bool flag = false;
			for (int i = 0; i < num; i++)
			{
				if (clientViewmodelInstance.AnimatorStateInfo(i).tagHash == AnimatorConditionalOverride.HandlingTag)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				this._wasReloading = false;
				return false;
			}
			if (this.IsReloading(firearm))
			{
				this._wasReloading = true;
				return true;
			}
			return !this._wasReloading;
		}

		public void Update(Firearm firearm, bool enabled)
		{
			if (!firearm.HasViewmodel)
			{
				return;
			}
			if (this._disableWhenHandling && this.IsHandling(firearm))
			{
				this._handlingElapsed += Time.deltaTime;
			}
			else
			{
				this._handlingElapsed = 0f;
			}
			float num;
			if (enabled && this._handlingElapsed < 0.2f)
			{
				num = ((this._enableSpeed <= 0f) ? 1f : (this._enableSpeed * Time.deltaTime));
			}
			else
			{
				num = ((this._disableSpeed <= 0f) ? (-1f) : (-this._disableSpeed * Time.deltaTime));
			}
			this._weight = Mathf.Clamp01(num + this._weight);
			this._mask.SetWeight(new Action<int, float>(firearm.ClientViewmodelInstance.AnimatorSetLayerWeight), this._weight);
		}

		[SerializeField]
		private AnimatorLayerMask _mask;

		[SerializeField]
		private float _enableSpeed;

		[SerializeField]
		private float _disableSpeed;

		[SerializeField]
		private bool _disableWhenHandling;

		private float _weight;

		private float _handlingElapsed;

		private bool _wasReloading;

		private const float HandlingTransitionCompensation = 0.2f;

		private static readonly int HandlingTag = FirearmAnimatorHashes.Reload;
	}
}
