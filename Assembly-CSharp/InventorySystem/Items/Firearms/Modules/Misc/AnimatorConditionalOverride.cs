using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

[Serializable]
public class AnimatorConditionalOverride
{
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

	private bool IsReloading(Firearm firearm)
	{
		if (firearm.TryGetModule<IReloaderModule>(out var module))
		{
			return module.IsReloadingOrUnloading;
		}
		return false;
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
		if (flag)
		{
			if (this.IsReloading(firearm))
			{
				this._wasReloading = true;
				return true;
			}
			return !this._wasReloading;
		}
		this._wasReloading = false;
		return false;
	}

	public void Update(Firearm firearm, bool enabled)
	{
		if (firearm.HasViewmodel)
		{
			if (this._disableWhenHandling && this.IsHandling(firearm))
			{
				this._handlingElapsed += Time.deltaTime;
			}
			else
			{
				this._handlingElapsed = 0f;
			}
			float num = ((!enabled || !(this._handlingElapsed < 0.2f)) ? ((this._disableSpeed <= 0f) ? (-1f) : ((0f - this._disableSpeed) * Time.deltaTime)) : ((this._enableSpeed <= 0f) ? 1f : (this._enableSpeed * Time.deltaTime)));
			this._weight = Mathf.Clamp01(num + this._weight);
			this._mask.SetWeight(firearm.ClientViewmodelInstance.AnimatorSetLayerWeight, this._weight);
		}
	}
}
