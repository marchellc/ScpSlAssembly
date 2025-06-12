using CameraShaking;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class RecoilPatternModule : ModuleBase, IDisplayableRecoilProviderModule
{
	private SubsequentShotsCounter _counter;

	[field: SerializeField]
	public RecoilSettings BaseRecoil { get; private set; }

	[field: SerializeField]
	public float AdsRecoilScale { get; private set; }

	[field: SerializeField]
	public AnimationCurve ZAxisScale { get; private set; }

	[field: SerializeField]
	public AnimationCurve FovKickScale { get; private set; }

	[field: SerializeField]
	public AnimationCurve HorizontalKickScale { get; private set; }

	[field: SerializeField]
	public AnimationCurve VerticalKickScale { get; private set; }

	public float DisplayHipRecoilDegrees
	{
		get
		{
			float scale = base.Firearm.AttachmentsValue(AttachmentParam.OverallRecoilMultiplier);
			float num = 0f;
			for (int i = 0; i < 10; i++)
			{
				RecoilSettings recoilSettings = this.Evaluate(i, scale);
				num += new Vector2(recoilSettings.SideKick, recoilSettings.UpKick).magnitude;
			}
			return num / 10f;
		}
	}

	public float DisplayAdsRecoilDegrees => this.HipToAds(this.DisplayHipRecoilDegrees);

	public virtual RecoilSettings Evaluate(int numberOfShots, float scale = 1f)
	{
		ModuleBase[] modules = base.Firearm.Modules;
		for (int i = 0; i < modules.Length; i++)
		{
			if (modules[i] is IRecoilScalingModule recoilScalingModule)
			{
				scale *= recoilScalingModule.RecoilMultiplier;
			}
		}
		return new RecoilSettings(this.BaseRecoil.AnimationTime, this.BaseRecoil.ZAxis * this.ZAxisScale.Evaluate(numberOfShots) * scale, (this.BaseRecoil.FovKick - 1f) * this.FovKickScale.Evaluate(numberOfShots) * scale + 1f, this.BaseRecoil.UpKick * this.VerticalKickScale.Evaluate(numberOfShots) * scale, this.BaseRecoil.SideKick * this.HorizontalKickScale.Evaluate(numberOfShots) * scale);
	}

	internal override void OnAdded()
	{
		base.OnAdded();
		if (base.Firearm.IsLocalPlayer)
		{
			this._counter = new SubsequentShotsCounter(base.Firearm);
			this._counter.OnShotRecorded += OnShot;
		}
	}

	private float HipToAds(float hip)
	{
		float modifierValue = base.Firearm.AttachmentsValue(AttachmentParam.AdsRecoilMultiplier);
		float num = AttachmentsUtils.MixValue(this.AdsRecoilScale, modifierValue, ParameterMixingMode.Percent);
		return hip * num;
	}

	private void OnShot()
	{
		int subsequentShots = this._counter.SubsequentShots;
		IAdsModule module;
		float t = (base.Firearm.TryGetModule<IAdsModule>(out module) ? module.AdsAmount : 0f);
		float num = base.Firearm.AttachmentsValue(AttachmentParam.OverallRecoilMultiplier);
		float b = this.HipToAds(num);
		CameraShakeController.AddEffect(new RecoilShake(this.Evaluate(subsequentShots, Mathf.Lerp(num, b, t))));
	}

	private void Update()
	{
		this._counter?.Update();
	}

	private void OnDestroy()
	{
		this._counter?.Destruct();
	}
}
