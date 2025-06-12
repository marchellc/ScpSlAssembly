using System.Collections.Generic;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.SwayControllers;

namespace InventorySystem.Items.Firearms;

public class FirearmSway : WalkSway
{
	private readonly bool _supportsAds;

	private readonly ViewmodelAdsExtension _adsExtension;

	private readonly AnimatedFirearmViewmodel _viewmodel;

	private readonly Firearm _firearm;

	private readonly IAdsModule _adsModule;

	private readonly List<ISwayModifierModule> _swayModifiers;

	private float _bobScale;

	private float _jumpScale;

	private float _walkScale;

	protected override GoopSwaySettings Settings
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

	protected override float OverallBobMultiplier => base.OverallBobMultiplier * this._bobScale;

	protected override float JumpSwayWeightMultiplier => base.JumpSwayWeightMultiplier * this._jumpScale;

	protected override float WalkSwayWeightMultiplier => base.WalkSwayWeightMultiplier * this._walkScale;

	public FirearmSway(GoopSwaySettings hipSettings, AnimatedFirearmViewmodel vm)
		: base(hipSettings, vm)
	{
		this._viewmodel = vm;
		this._firearm = vm.ParentFirearm;
		this._supportsAds = this.TryInitAds(out this._adsExtension, out this._adsModule);
		this._swayModifiers = new List<ISwayModifierModule>();
		ModuleBase[] modules = this._firearm.Modules;
		for (int i = 0; i < modules.Length; i++)
		{
			if (modules[i] is ISwayModifierModule item)
			{
				this._swayModifiers.Add(item);
			}
		}
	}

	private bool TryInitAds(out ViewmodelAdsExtension extension, out IAdsModule module)
	{
		bool num = this._viewmodel.TryGetExtension<ViewmodelAdsExtension>(out extension);
		bool flag = this._firearm.TryGetModule<IAdsModule>(out module);
		return num && flag;
	}

	public override void UpdateSway()
	{
		base.UpdateSway();
		this._bobScale = 1f;
		this._jumpScale = 1f;
		this._walkScale = 1f;
		foreach (ISwayModifierModule swayModifier in this._swayModifiers)
		{
			this._bobScale *= swayModifier.BobbingSwayScale;
			this._jumpScale *= swayModifier.JumpSwayScale;
			this._walkScale *= swayModifier.WalkSwayScale;
		}
	}
}
