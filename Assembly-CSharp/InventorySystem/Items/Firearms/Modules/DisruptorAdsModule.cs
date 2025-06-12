using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class DisruptorAdsModule : LinearAdsModule, ILightEmittingItem
{
	private const float NightVisionThreshold = 0.6f;

	private DisruptorActionModule _disruptorAction;

	private bool _preventAds;

	[SerializeField]
	private float _zoomAmount;

	[SerializeField]
	private float _sensitivtyScale;

	[SerializeField]
	private float _inaccuracyScale;

	protected override bool AllowAds
	{
		get
		{
			if (base.AllowAds && !this._disruptorAction.IsReloading)
			{
				return !this._preventAds;
			}
			return false;
		}
	}

	public override float BaseZoomAmount => this._zoomAmount;

	public override float AdditionalSensitivityModifier => this._sensitivtyScale;

	public override float BaseAdsInaccuracy => base.BaseAdsInaccuracy * this._inaccuracyScale;

	public override float BaseHipInaccuracy => base.BaseHipInaccuracy * this._inaccuracyScale;

	public bool IsEmittingLight => base.AdsAmount > 0.6f;

	protected override void OnInit()
	{
		base.OnInit();
		base.Firearm.TryGetModule<DisruptorActionModule>(out this._disruptorAction);
	}

	[ExposedFirearmEvent]
	public void ForceExitAds()
	{
		this._preventAds = true;
	}
}
