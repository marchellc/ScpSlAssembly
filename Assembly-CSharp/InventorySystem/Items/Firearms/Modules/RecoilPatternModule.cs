using System;
using CameraShaking;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class RecoilPatternModule : ModuleBase, IDisplayableRecoilProviderModule
	{
		public RecoilSettings BaseRecoil { get; private set; }

		public float AdsRecoilScale { get; private set; }

		public AnimationCurve ZAxisScale { get; private set; }

		public AnimationCurve FovKickScale { get; private set; }

		public AnimationCurve HorizontalKickScale { get; private set; }

		public AnimationCurve VerticalKickScale { get; private set; }

		public float DisplayHipRecoilDegrees
		{
			get
			{
				float num = base.Firearm.AttachmentsValue(AttachmentParam.OverallRecoilMultiplier);
				float num2 = 0f;
				for (int i = 0; i < 10; i++)
				{
					RecoilSettings recoilSettings = this.Evaluate(i, num);
					num2 += new Vector2(recoilSettings.SideKick, recoilSettings.UpKick).magnitude;
				}
				return num2 / 10f;
			}
		}

		public float DisplayAdsRecoilDegrees
		{
			get
			{
				return this.HipToAds(this.DisplayHipRecoilDegrees);
			}
		}

		public virtual RecoilSettings Evaluate(int numberOfShots, float scale = 1f)
		{
			ModuleBase[] modules = base.Firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				IRecoilScalingModule recoilScalingModule = modules[i] as IRecoilScalingModule;
				if (recoilScalingModule != null)
				{
					scale *= recoilScalingModule.RecoilMultiplier;
				}
			}
			return new RecoilSettings(this.BaseRecoil.AnimationTime, this.BaseRecoil.ZAxis * this.ZAxisScale.Evaluate((float)numberOfShots) * scale, (this.BaseRecoil.FovKick - 1f) * this.FovKickScale.Evaluate((float)numberOfShots) * scale + 1f, this.BaseRecoil.UpKick * this.VerticalKickScale.Evaluate((float)numberOfShots) * scale, this.BaseRecoil.SideKick * this.HorizontalKickScale.Evaluate((float)numberOfShots) * scale);
		}

		internal override void OnAdded()
		{
			base.OnAdded();
			if (!base.Firearm.IsLocalPlayer)
			{
				return;
			}
			this._counter = new SubsequentShotsCounter(base.Firearm, 1f, 0.1f, 0.4f);
			this._counter.OnShotRecorded += this.OnShot;
		}

		private float HipToAds(float hip)
		{
			float num = base.Firearm.AttachmentsValue(AttachmentParam.AdsRecoilMultiplier);
			float num2 = AttachmentsUtils.MixValue(this.AdsRecoilScale, num, ParameterMixingMode.Percent);
			return hip * num2;
		}

		private void OnShot()
		{
			int subsequentShots = this._counter.SubsequentShots;
			IAdsModule adsModule;
			float num = (base.Firearm.TryGetModule(out adsModule, true) ? adsModule.AdsAmount : 0f);
			float num2 = base.Firearm.AttachmentsValue(AttachmentParam.OverallRecoilMultiplier);
			float num3 = this.HipToAds(num2);
			CameraShakeController.AddEffect(new RecoilShake(this.Evaluate(subsequentShots, Mathf.Lerp(num2, num3, num))));
		}

		private void Update()
		{
			SubsequentShotsCounter counter = this._counter;
			if (counter == null)
			{
				return;
			}
			counter.Update();
		}

		private void OnDestroy()
		{
			SubsequentShotsCounter counter = this._counter;
			if (counter == null)
			{
				return;
			}
			counter.Destruct();
		}

		private SubsequentShotsCounter _counter;
	}
}
