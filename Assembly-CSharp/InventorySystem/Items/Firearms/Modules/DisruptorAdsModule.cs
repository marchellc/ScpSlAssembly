using System;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class DisruptorAdsModule : LinearAdsModule, ILightEmittingItem
	{
		protected override bool AllowAds
		{
			get
			{
				return base.AllowAds && !this._disruptorAction.IsReloading && !this._preventAds;
			}
		}

		public override float BaseZoomAmount
		{
			get
			{
				return this._zoomAmount;
			}
		}

		public override float AdditionalSensitivityModifier
		{
			get
			{
				return this._sensitivtyScale;
			}
		}

		public override float BaseAdsInaccuracy
		{
			get
			{
				return base.BaseAdsInaccuracy * this._inaccuracyScale;
			}
		}

		public override float BaseHipInaccuracy
		{
			get
			{
				return base.BaseHipInaccuracy * this._inaccuracyScale;
			}
		}

		public bool IsEmittingLight
		{
			get
			{
				return base.AdsAmount > 0.6f;
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			base.Firearm.TryGetModule(out this._disruptorAction, true);
		}

		[ExposedFirearmEvent]
		public void ForceExitAds()
		{
			this._preventAds = true;
		}

		private const float NightVisionThreshold = 0.6f;

		private DisruptorActionModule _disruptorAction;

		private bool _preventAds;

		[SerializeField]
		private float _zoomAmount;

		[SerializeField]
		private float _sensitivtyScale;

		[SerializeField]
		private float _inaccuracyScale;
	}
}
