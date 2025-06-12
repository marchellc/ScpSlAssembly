using System;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ViewmodelReloadOnlyLayerExtension : MonoBehaviour, IViewmodelExtension
{
	private enum Mode
	{
		DuringReloadsAndUnloads,
		OutsideReloadsAndUnloads
	}

	[SerializeField]
	private AnimatorLayerMask _mask;

	[Header("When should the layers be active?")]
	[SerializeField]
	private Mode _mode;

	private AnimatedFirearmViewmodel _viewmodel;

	private IReloaderModule _reloaderModule;

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._viewmodel = viewmodel;
		this._viewmodel.ParentFirearm.TryGetModule<IReloaderModule>(out this._reloaderModule);
	}

	private void Update()
	{
		bool isReloadingOrUnloading = this._reloaderModule.IsReloadingOrUnloading;
		bool flag = this._mode switch
		{
			Mode.DuringReloadsAndUnloads => isReloadingOrUnloading, 
			Mode.OutsideReloadsAndUnloads => !isReloadingOrUnloading, 
			_ => throw new NotImplementedException("Unknown operating mode of ViewmodelReloadOnlyLayerExtension"), 
		};
		this._viewmodel.AnimatorSetLayerWeight(this._mask, flag ? 1 : 0);
	}
}
