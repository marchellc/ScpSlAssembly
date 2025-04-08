using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.SwayControllers;

namespace InventorySystem.Items.Firearms
{
	public class FirearmSway : WalkSway
	{
		protected override GoopSway.GoopSwaySettings Settings
		{
			get
			{
				if (!this._supportsAds || !this._adsModule.AdsTarget)
				{
					return base.Settings;
				}
				return this._adsExtension.AdsSway;
			}
		}

		protected override float OverallBobMultiplier
		{
			get
			{
				return base.OverallBobMultiplier * this._bobScale;
			}
		}

		protected override float JumpSwayWeightMultiplier
		{
			get
			{
				return base.JumpSwayWeightMultiplier * this._jumpScale;
			}
		}

		protected override float WalkSwayWeightMultiplier
		{
			get
			{
				return base.WalkSwayWeightMultiplier * this._walkScale;
			}
		}

		public FirearmSway(GoopSway.GoopSwaySettings hipSettings, AnimatedFirearmViewmodel vm)
			: base(hipSettings, vm)
		{
			this._viewmodel = vm;
			this._firearm = vm.ParentFirearm;
			this._supportsAds = this.TryInitAds(out this._adsExtension, out this._adsModule);
			this._swayModifiers = new List<ISwayModifierModule>();
			ModuleBase[] modules = this._firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				ISwayModifierModule swayModifierModule = modules[i] as ISwayModifierModule;
				if (swayModifierModule != null)
				{
					this._swayModifiers.Add(swayModifierModule);
				}
			}
		}

		private bool TryInitAds(out ViewmodelAdsExtension extension, out IAdsModule module)
		{
			bool flag = this._viewmodel.TryGetExtension<ViewmodelAdsExtension>(out extension);
			bool flag2 = this._firearm.TryGetModule(out module, true);
			return flag && flag2;
		}

		public override void UpdateSway()
		{
			base.UpdateSway();
			this._bobScale = 1f;
			this._jumpScale = 1f;
			this._walkScale = 1f;
			foreach (ISwayModifierModule swayModifierModule in this._swayModifiers)
			{
				this._bobScale *= swayModifierModule.BobbingSwayScale;
				this._jumpScale *= swayModifierModule.JumpSwayScale;
				this._walkScale *= swayModifierModule.WalkSwayScale;
			}
		}

		private readonly bool _supportsAds;

		private readonly ViewmodelAdsExtension _adsExtension;

		private readonly AnimatedFirearmViewmodel _viewmodel;

		private readonly Firearm _firearm;

		private readonly IAdsModule _adsModule;

		private readonly List<ISwayModifierModule> _swayModifiers;

		private float _bobScale;

		private float _jumpScale;

		private float _walkScale;
	}
}
