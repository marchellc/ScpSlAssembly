using System;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class ViewmodelReloadOnlyLayerExtension : MonoBehaviour, IViewmodelExtension
	{
		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._viewmodel = viewmodel;
			this._viewmodel.ParentFirearm.TryGetModule(out this._reloaderModule, true);
		}

		private void Update()
		{
			bool isReloadingOrUnloading = this._reloaderModule.IsReloadingOrUnloading;
			ViewmodelReloadOnlyLayerExtension.Mode mode = this._mode;
			bool flag;
			if (mode != ViewmodelReloadOnlyLayerExtension.Mode.DuringReloadsAndUnloads)
			{
				if (mode != ViewmodelReloadOnlyLayerExtension.Mode.OutsideReloadsAndUnloads)
				{
					throw new NotImplementedException("Unknown operating mode of ViewmodelReloadOnlyLayerExtension");
				}
				flag = !isReloadingOrUnloading;
			}
			else
			{
				flag = isReloadingOrUnloading;
			}
			bool flag2 = flag;
			this._viewmodel.AnimatorSetLayerWeight(this._mask, (float)(flag2 ? 1 : 0));
		}

		[SerializeField]
		private AnimatorLayerMask _mask;

		[Header("When should the layers be active?")]
		[SerializeField]
		private ViewmodelReloadOnlyLayerExtension.Mode _mode;

		private AnimatedFirearmViewmodel _viewmodel;

		private IReloaderModule _reloaderModule;

		private enum Mode
		{
			DuringReloadsAndUnloads,
			OutsideReloadsAndUnloads
		}
	}
}
